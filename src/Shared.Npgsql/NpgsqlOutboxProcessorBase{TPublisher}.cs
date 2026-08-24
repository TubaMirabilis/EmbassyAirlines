using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Abstractions;

namespace Shared.Npgsql;

public abstract class NpgsqlOutboxProcessorBase<TPublisher> : OutboxProcessorBase<TPublisher>, IOutboxProcessor
{
    protected NpgsqlOutboxProcessorBase(DbContext dbContext, TPublisher publisher, ILogger logger) : base(publisher, logger) => DbContext = dbContext;
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
    // Selects the messages that are due, stamps them with this invocation's claim identifier and a lease that
    // expires OutboxConstants.ClaimDuration after the database's own clock, and returns the stamped rows. Doing all
    // of that in one statement keeps the lease PostgreSQL's to grant: no application timestamp reaches the row.
    protected abstract Task<List<OutboxMessage>> ClaimEligibleMessagesAsync(Guid claimId, CancellationToken cancellationToken);
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
        Logger.LogInformation("Claimed {ClaimedCount} outbox message(s) as {ClaimId} until {ClaimedUntilUtc:o}", messages.Count, claimId, messages[0].ClaimedUntilUtc);
        return new ClaimedBatch(claimId, claimIssuedAt, messages);
    }
    private async Task<BatchResult> ProcessBatchAsync(ClaimedBatch batch, CancellationToken cancellationToken)
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
            if (await ProcessMessageAsync(message, DateTime.UtcNow, cancellationToken))
            {
                publishedCount++;
            }
            var outcome = await RecordOutcomeAsync(message, batch.ClaimId);
            if (outcome is OutcomeResult.Recorded)
            {
                continue;
            }
            LogAbandonedMessages(batch.Messages.Count - attemptedCount, GetOutcomeFailureReason(outcome));
            break;
        }
        return new BatchResult(publishedCount, attemptedCount);
    }
    private async Task<OutcomeResult> RecordOutcomeAsync(OutboxMessage message, Guid claimId)
    {
        // The outcome may only be written while this worker still holds the claim, so the lease is re-checked inside
        // the UPDATE itself, against the database clock that granted it. Matching on ClaimId alone would let a worker
        // whose lease expired while it was publishing write its outcome, provided no other worker had reclaimed the
        // row yet; comparing the expiry to this process's clock would make ownership depend on how far that clock had
        // drifted from PostgreSQL's.
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
            // Publishing carries on being at-least-once around this boundary whatever happens, but there is no reason
            // to widen that window once the database has shown it cannot record outcomes. Every message published from
            // here on would risk the same fate, so the rest of the claim is left for a later invocation to retry.
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
    private sealed record ClaimedBatch(Guid ClaimId, long ClaimIssuedAt, IReadOnlyList<OutboxMessage> Messages);
    private readonly record struct BatchResult(int PublishedCount, int AttemptedCount);
    private enum OutcomeResult
    {
        Recorded,
        ClaimLost,
        PersistenceFailed
    }
}
