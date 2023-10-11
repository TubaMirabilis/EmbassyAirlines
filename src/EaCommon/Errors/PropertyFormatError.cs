using FluentResults;

namespace EaCommon.Errors;

public class PropertyFormatError : IError
{
    public PropertyFormatError(string message)
    {
        Message = message;
    }
    public string Message { get; private set; }
    public List<IError> Reasons => throw new NotImplementedException();
    public Dictionary<string, object> Metadata => throw new NotImplementedException();
}