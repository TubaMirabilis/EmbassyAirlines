using AWS.Messaging;
using Flights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Contracts;
using Shared.Npgsql;

namespace Flights.Infrastructure.Outbox;

public sealed class OutboxProcessor : NpgsqlOutboxProcessorBase<IMessagePublisher>
{
    private static readonly Dictionary<string, Func<IMessagePublisher, string, CancellationToken, Task>> s_publishers =
        new(StringComparer.Ordinal)
        {
            [nameof(FlightCancelledEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<FlightCancelledEvent>(content), ct),
            [nameof(FlightArrivedEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<FlightArrivedEvent>(content), ct),
            [nameof(FlightDelayedEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<FlightDelayedEvent>(content), ct),
            [nameof(FlightMarkedAsDelayedEnRouteEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<FlightMarkedAsDelayedEnRouteEvent>(content), ct),
            [nameof(FlightMarkedAsEnRouteEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<FlightMarkedAsEnRouteEvent>(content), ct),
            [nameof(FlightRescheduledEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<FlightRescheduledEvent>(content), ct),
            [nameof(FlightPricingAdjustedEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<FlightPricingAdjustedEvent>(content), ct),
            [nameof(AircraftAssignedToFlightEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<AircraftAssignedToFlightEvent>(content), ct),
            [nameof(FlightScheduledEvent)] = (publisher, content, ct) => publisher.PublishAsync(Deserialize<FlightScheduledEvent>(content), ct)
        };
    public OutboxProcessor(ApplicationDbContext dbContext,
                           IMessagePublisher publisher,
                           ILogger<OutboxProcessor> logger) : base(dbContext, publisher, logger)
    {
    }
    protected override Task<List<OutboxMessage>> ClaimEligibleMessagesAsync(Guid claimId, CancellationToken cancellationToken) => DbContext.Set<OutboxMessage>().FromSql(
    $"""
        WITH due AS (
            SELECT id FROM flights.outbox_messages
            WHERE processed_on_utc IS NULL
            AND dead_lettered_on_utc IS NULL
            AND (claimed_until_utc IS NULL OR claimed_until_utc <= now())
            AND (next_attempt_on_utc IS NULL OR next_attempt_on_utc <= now())
            ORDER BY created_on_utc
            LIMIT {OutboxConstants.BatchSize}
            FOR UPDATE SKIP LOCKED
        )
        UPDATE flights.outbox_messages AS m
        SET claim_id = {claimId}, claimed_until_utc = now() + {OutboxConstants.ClaimDuration}
        FROM due
        WHERE m.id = due.id
        RETURNING m.*
    """).ToListAsync(cancellationToken);
    protected override Func<IMessagePublisher, string, CancellationToken, Task>? ResolvePublisher(string messageName)
        => s_publishers.GetValueOrDefault(messageName);
}
