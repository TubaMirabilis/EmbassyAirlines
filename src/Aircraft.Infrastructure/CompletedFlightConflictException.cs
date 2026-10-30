namespace Aircraft.Infrastructure;

public sealed class CompletedFlightConflictException : Exception
{
    public CompletedFlightConflictException()
    {
    }

    public CompletedFlightConflictException(string message)
        : base(message)
    {
    }

    public CompletedFlightConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
