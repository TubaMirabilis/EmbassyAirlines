using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.TransactionalOutbox;

namespace Shared.Npgsql;

public abstract class NpgsqlOutboxProcessorBase<TPublisher> : OutboxProcessorBase<TPublisher>, IOutboxProcessor
{
    private const int MaxIdentifierLength = 63;
    private readonly string _claimEligibleMessagesSql;
    protected NpgsqlOutboxProcessorBase(DbContext dbContext, string schema, TPublisher publisher, ILogger logger) : base(publisher, logger)
    {
        DbContext = dbContext;
        _claimEligibleMessagesSql = CreateClaimEligibleMessagesSql(QuoteSchema(schema));
    }
    protected DbContext DbContext { get; }
    public async Task<int> ProcessAsync(CancellationToken cancellationToken = default)
    {
        var batch = await ClaimBatchAsync(cancellationToken);
        if (batch is null)
        {
            return 0;
        }
        var result = await ProcessBatchAsync(batch, cancellationToken);
        LogBatchResult(result.PublishedCount, result.AttemptedCount);
        return result.PublishedCount;
    }
    private static string QuoteSchema(string schema)
    {
        Ensure.NotNullOrEmpty(schema);
        if (schema.Length > MaxIdentifierLength
            || char.IsAsciiDigit(schema[0])
            || !schema.All(static c => char.IsAsciiLetterOrDigit(c) || c == '_'))
        {
            throw new ArgumentException($"Schema name '{schema}' must be at most {MaxIdentifierLength} characters, start with an ASCII letter or underscore, and contain only ASCII letters, digits, and underscores.", nameof(schema));
        }
        return $"\"{schema}\"";
    }
    // Identifiers cannot be bound as parameters, so the validated, quoted schema is embedded in the SQL;
    // {0}..{2} remain placeholders that FromSqlRaw binds as parameters.
    private static string CreateClaimEligibleMessagesSql(string quotedSchema) =>
    $$"""
        WITH due AS (
            SELECT id FROM {{quotedSchema}}.outbox_messages
            WHERE processed_on_utc IS NULL
            AND dead_lettered_on_utc IS NULL
            AND (claimed_until_utc IS NULL OR claimed_until_utc <= now())
            AND (next_attempt_on_utc IS NULL OR next_attempt_on_utc <= now())
            ORDER BY created_on_utc, id
            LIMIT {0}
            FOR UPDATE SKIP LOCKED
        )
        UPDATE {{quotedSchema}}.outbox_messages AS m
        SET claim_id = {1}, claimed_until_utc = now() + {2}
        FROM due
        WHERE m.id = due.id
        RETURNING m.*
    """;
    private Task<List<OutboxMessage>> ClaimEligibleMessagesAsync(Guid claimId, CancellationToken cancellationToken) => DbContext.Set<OutboxMessage>()
        .FromSqlRaw(_claimEligibleMessagesSql, OutboxConstants.BatchSize, claimId, OutboxConstants.ClaimDuration)
        .ToListAsync(cancellationToken);
    private async Task<ClaimedBatch?> ClaimBatchAsync(CancellationToken cancellationToken)
    {
        var claimId = Guid.CreateVersion7();
        var claimIssuedAt = Stopwatch.GetTimestamp();
        var messages = await ClaimEligibleMessagesAsync(claimId, cancellationToken);
        if (messages.Count == 0)
        {
            return null;
        }
        messages.Sort(static (left, right) => (left.CreatedOnUtc, left.Id).CompareTo((right.CreatedOnUtc, right.Id)));
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Claimed {ClaimedCount} outbox message(s) as {ClaimId} until {ClaimedUntilUtc:o}", messages.Count, claimId, messages[0].ClaimedUntilUtc);
        }
        return new ClaimedBatch(claimId, claimIssuedAt, messages);
    }
    private async Task<BatchResult> ProcessBatchAsync(
    ClaimedBatch batch,
    CancellationToken cancellationToken)
    {
        var publishedCount = 0;
        var attemptedCount = 0;
        foreach (var message in batch.Messages)
        {
            var stopReason = GetStopReason(batch.ClaimIssuedAt, cancellationToken);
            if (stopReason is not null)
            {
                LogAbandonedMessages(batch.Messages.Count - attemptedCount, stopReason);
                break;
            }
            attemptedCount++;
            publishedCount += await ProcessMessageAsync(
                message,
                DateTime.UtcNow,
                cancellationToken) ? 1 : 0;
            var outcome = await RecordOutcomeAsync(message, batch.ClaimId);
            if (outcome is not OutcomeResult.Recorded)
            {
                LogAbandonedMessages(
                    batch.Messages.Count - attemptedCount,
                    GetOutcomeFailureReason(outcome));
                break;
            }
        }
        return new(publishedCount, attemptedCount);
    }
    private async Task<OutcomeResult> RecordOutcomeAsync(OutboxMessage message, Guid claimId)
    {
        DbContext.Entry(message).State = EntityState.Detached;
        try
        {
            var updatedCount = await DbContext.Set<OutboxMessage>()
                .Where(m => m.Id == message.Id && m.ClaimId == claimId && m.ClaimedUntilUtc > DatabaseClock.UtcNow())
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.ProcessedOnUtc, message.ProcessedOnUtc)
                    .SetProperty(m => m.Error, message.Error)
                    .SetProperty(m => m.RetryCount, message.RetryCount)
                    .SetProperty(m => m.NextAttemptOnUtc, message.NextAttemptOnUtc)
                    .SetProperty(m => m.DeadLetteredOnUtc, message.DeadLetteredOnUtc)
                    .SetProperty(m => m.ClaimId, (Guid?)null)
                    .SetProperty(m => m.ClaimedUntilUtc, (DateTime?)null), CancellationToken.None);
            if (updatedCount > 0)
            {
                return OutcomeResult.Recorded;
            }
            Logger.LogError("Outbox message {MessageId} of type {MessageType} is no longer validly claimed by {ClaimId}; its outcome was discarded because the claim expired or another worker now owns it", message.Id, message.Name, claimId);
            return OutcomeResult.ClaimLost;
        }
        catch (DbException e)
        {
            Logger.LogError(e, "Failed to record the outcome of outbox message {MessageId} of type {MessageType}; it will be reprocessed once its claim expires", message.Id, message.Name);
            return OutcomeResult.PersistenceFailed;
        }
    }
    private static string? GetStopReason(long claimIssuedAt, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return "cancellation was requested";
        }
        var elapsed = Stopwatch.GetElapsedTime(claimIssuedAt);
        if (elapsed >= OutboxConstants.ClaimDuration - OutboxConstants.ClaimSafetyMargin)
        {
            return "the claim was too close to expiring to publish safely";
        }
        return null;
    }
    private static string GetOutcomeFailureReason(OutcomeResult outcome) => outcome switch
    {
        OutcomeResult.ClaimLost => "the claim was lost",
        OutcomeResult.PersistenceFailed => "the outcome of the preceding message could not be persisted",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
    };
    private void LogAbandonedMessages(int abandonedCount, string reason)
    {
        if (abandonedCount == 0)
        {
            return;
        }
        Logger.LogWarning("Stopped before {AbandonedCount} claimed outbox message(s) were attempted because {Reason}; they will be reprocessed once their claim expires", abandonedCount, reason);
    }
    private void LogBatchResult(int publishedCount, int attemptedCount)
    {
        if (!Logger.IsEnabled(LogLevel.Information))
        {
            return;
        }
        Logger.LogInformation("Published {PublishedCount} of {AttemptedCount} attempted outbox message(s)", publishedCount, attemptedCount);
    }
    private sealed record ClaimedBatch(Guid ClaimId, long ClaimIssuedAt, IReadOnlyList<OutboxMessage> Messages);
    private readonly record struct BatchResult(int PublishedCount, int AttemptedCount);
    private enum OutcomeResult
    {
        Recorded,
        ClaimLost,
        PersistenceFailed
    }
}
