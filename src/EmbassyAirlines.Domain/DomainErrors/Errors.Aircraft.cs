using ErrorOr;

namespace EmbassyAirlines.Domain.DomainErrors;

public static partial class Errors
{
    public static class Aircraft
    {
        public static Error NotFound => Error.NotFound(
            code: "Aircraft.NotFound",
            description: "Aircraft with given ID does not exist");
    }
}