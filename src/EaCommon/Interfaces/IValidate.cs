using FluentResults;
using Mediator;

namespace EaCommon.Interfaces;

public interface IValidate : IMessage
{
    Result Validate();
}