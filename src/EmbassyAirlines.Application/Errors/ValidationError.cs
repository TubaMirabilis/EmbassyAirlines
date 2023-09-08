namespace EmbassyAirlines.Application.Errors;

public sealed record ValidationError(IEnumerable<string> Errors);