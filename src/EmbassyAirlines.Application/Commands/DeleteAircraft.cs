using FluentResults;
using Mediator;

namespace EmbassyAirlines.Application.Commands;

public sealed record DeleteAircraft(Guid Id) : ICommand<Result<Unit>>;