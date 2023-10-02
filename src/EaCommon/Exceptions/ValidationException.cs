using EmbassyAirlines.Application.Errors;

namespace EmbassyAirlines.Application.Exceptions;

public sealed class ValidationException : Exception
{
    public ValidationException(ValidationError validationError) : base("Validation error") =>
        ValidationError = validationError;

    public ValidationError ValidationError { get; }
}
