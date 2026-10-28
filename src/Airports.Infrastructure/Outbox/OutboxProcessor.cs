using Airports.Infrastructure.Database;
using AWS.Messaging;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.Npgsql;
using Shared.TransactionalOutbox;

namespace Airports.Infrastructure.Outbox;

public sealed class OutboxProcessor : NpgsqlOutboxProcessorBase<IMessagePublisher>
{
    private static readonly Dictionary<string, Func<IMessagePublisher, string, CancellationToken, Task>> s_publishers =
        new(StringComparer.Ordinal)
        {
            [nameof(AirportCreatedEvent)] = (publisher, content, ct) => publisher.PublishAsync(OutboxJson.Deserialize<AirportCreatedEvent>(content), ct),
            [nameof(AirportUpdatedEvent)] = (publisher, content, ct) => publisher.PublishAsync(OutboxJson.Deserialize<AirportUpdatedEvent>(content), ct)
        };
    public OutboxProcessor(ApplicationDbContext dbContext,
                           IMessagePublisher publisher,
                           ILogger<OutboxProcessor> logger) : base(dbContext, "airports", publisher, logger)
    {
    }
    protected override Func<IMessagePublisher, string, CancellationToken, Task>? ResolvePublisher(string messageName)
        => s_publishers.GetValueOrDefault(messageName);
}
