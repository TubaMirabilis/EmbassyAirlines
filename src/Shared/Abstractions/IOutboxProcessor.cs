namespace Shared.Abstractions;

public interface IOutboxProcessor
{
    Task<int> ProcessAsync(CancellationToken cancellationToken = default);
}
