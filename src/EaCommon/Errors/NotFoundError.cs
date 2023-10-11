using FluentResults;

namespace EaCommon.Errors;

public class NotFoundError : IError
{
    public NotFoundError(string entity)
    {
        Message = $"{entity} not found";
    }
    public string Message { get; private set; }
    public List<IError> Reasons => throw new NotImplementedException();
    public Dictionary<string, object> Metadata => throw new NotImplementedException();
}