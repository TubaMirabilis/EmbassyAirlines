using System;

namespace EmbassyAirlines.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException() : base("The requested resource was not found.")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}