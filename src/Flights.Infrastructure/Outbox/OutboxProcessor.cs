using AWS.Messaging;
using Flights.Infrastructure.Database;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.Npgsql;
using Shared.TransactionalOutbox;

namespace Flights.Infrastructure.Outbox;

public sealed class OutboxProcessor : NpgsqlOutboxProcessorBase<IMessagePublisher>
{
    private static readonly Dictionary<string, Func<IMessagePublisher, string, CancellationToken, Task>> s_publishers =
        new(StringComparer.Ordinal)
        {
            [nameof(FlightCancelledEvent)] = (publisher, content, ct) => publisher.PublishAsync(OutboxJson.Deserialize<FlightCancelledEvent>(content), ct),
            [nameof(FlightArrivedEvent)] = (publisher, content, ct) => publisher.PublishAsync(OutboxJson.Deserialize<FlightArrivedEvent>(content), ct),
            [nameof(FlightDelayedEvent)] = (publisher, content, ct) => publisher.PublishAsync(OutboxJson.Deserialize<FlightDelayedEvent>(content), ct),
            [nameof(FlightMarkedAsDelayedEnRouteEvent)] = (publisher, content, ct) => publisher.PublishAsync(OutboxJson.Deserialize<FlightMarkedAsDelayedEnRouteEvent>(content), ct),
            [nameof(FlightMarkedAsEnRouteEvent)] = (publisher, content, ct) => publisher.PublishAsync(OutboxJson.Deserialize<FlightMarkedAsEnRouteEvent>(content), ct),
            [nameof(FlightRescheduledEvent)] = (publisher, content, ct) => publisher.PublishAsync(OutboxJson.Deserialize<FlightRescheduledEvent>(content), ct),
            [nameof(FlightPricingAdjustedEvent)] = (publisher, content, ct) => publisher.PublishAsync(OutboxJson.Deserialize<FlightPricingAdjustedEvent>(content), ct),
            [nameof(AircraftAssignedToFlightEvent)] = (publisher, content, ct) => publisher.PublishAsync(OutboxJson.Deserialize<AircraftAssignedToFlightEvent>(content), ct),
            [nameof(FlightScheduledEvent)] = (publisher, content, ct) => publisher.PublishAsync(OutboxJson.Deserialize<FlightScheduledEvent>(content), ct)
        };
    public OutboxProcessor(ApplicationDbContext dbContext,
                           IMessagePublisher publisher,
                           ILogger<OutboxProcessor> logger) : base(dbContext, "flights", publisher, logger)
    {
    }
    protected override Func<IMessagePublisher, string, CancellationToken, Task>? ResolvePublisher(string messageName)
        => s_publishers.GetValueOrDefault(messageName);
}
