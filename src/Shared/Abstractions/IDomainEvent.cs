namespace Shared.Abstractions;

public interface IDomainEvent
{
    Guid Id { get; }
}
