namespace EaCommon.Errors;

public sealed record ValidationError(IEnumerable<string> Errors);