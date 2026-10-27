namespace Shared.TransactionalOutbox;

public interface IOutboxProcessor
{
    Task<int> ProcessAsync(CancellationToken cancellationToken = default);
}
