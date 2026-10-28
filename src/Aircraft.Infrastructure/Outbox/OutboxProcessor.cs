using Aircraft.Infrastructure.Database;
using AWS.Messaging;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.Npgsql;
using Shared.TransactionalOutbox;

namespace Aircraft.Infrastructure.Outbox;

public sealed class OutboxProcessor : NpgsqlOutboxProcessorBase<IMessagePublisher>
{
    private static readonly Dictionary<string, Func<IMessagePublisher, string, CancellationToken, Task>> s_publishers =
        new(StringComparer.Ordinal)
        {
            [nameof(AircraftCreatedEvent)] = (publisher, content, ct) => publisher.PublishAsync(OutboxJson.Deserialize<AircraftCreatedEvent>(content), ct)
        };
    public OutboxProcessor(ApplicationDbContext dbContext,
                           IMessagePublisher publisher,
                           ILogger<OutboxProcessor> logger) : base(dbContext, "aircraft", publisher, logger)
    {
    }
    protected override Func<IMessagePublisher, string, CancellationToken, Task>? ResolvePublisher(string messageName)
        => s_publishers.GetValueOrDefault(messageName);
}
