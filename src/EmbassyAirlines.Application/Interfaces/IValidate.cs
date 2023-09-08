using EmbassyAirlines.Application.Errors;
using Mediator;
using System.Diagnostics.CodeAnalysis;

namespace EmbassyAirlines.Application.Interfaces;

public interface IValidate : IMessage
{
    bool IsValid([NotNullWhen(false)] out ValidationError? error);
}