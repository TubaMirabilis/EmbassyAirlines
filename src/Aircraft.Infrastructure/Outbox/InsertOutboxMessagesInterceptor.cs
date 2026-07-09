using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared;

namespace Aircraft.Infrastructure.Outbox;

internal sealed class InsertOutboxMessagesInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions s_serializerOptions = new(JsonSerializerDefaults.Web);
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            InsertOutboxMessages(eventData.Context);
        }
        return base.SavingChanges(eventData, result);
    }
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            InsertOutboxMessages(eventData.Context);
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is not null)
        {
            ClearDomainEvents(eventData.Context);
        }
        return base.SavedChanges(eventData, result);
    }
    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            ClearDomainEvents(eventData.Context);
        }
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }
    private void InsertOutboxMessages(DbContext context)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var trackedMessageIds = context.ChangeTracker
                                       .Entries<OutboxMessage>()
                                       .Select(entry => entry.Entity.Id)
                                       .ToHashSet();
        var domainEvents = context.ChangeTracker
                                  .Entries<Entity>()
                                  .SelectMany(entry => entry.Entity.DomainEvents)
                                  .ToList();
        var outboxMessages = new List<OutboxMessage>();
        foreach (var domainEvent in domainEvents)
        {
            if (!trackedMessageIds.Add(domainEvent.Id))
            {
                continue;
            }
            outboxMessages.Add(new OutboxMessage(
                domainEvent.Id,
                domainEvent.GetType().Name,
                JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), s_serializerOptions),
                now));
        }
        if (outboxMessages.Count > 0)
        {
            context.Set<OutboxMessage>().AddRange(outboxMessages);
        }
    }
    private static void ClearDomainEvents(DbContext context)
    {
        foreach (var entity in context.ChangeTracker.Entries<Entity>().Select(entry => entry.Entity))
        {
            entity.ClearDomainEvents();
        }
    }
}
