using EaCommon.Errors;
using Mediator;
using System.Diagnostics.CodeAnalysis;

namespace EaCommon.Interfaces;

public interface IValidate : IMessage
{
    bool IsValid([NotNullWhen(false)] out ValidationError? error);
}