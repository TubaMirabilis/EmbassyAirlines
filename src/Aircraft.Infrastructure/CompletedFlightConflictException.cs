namespace Aircraft.Infrastructure;

public sealed class CompletedFlightConflictException : Exception
{
    public CompletedFlightConflictException(string message) : base(message)
    {
    }
}
