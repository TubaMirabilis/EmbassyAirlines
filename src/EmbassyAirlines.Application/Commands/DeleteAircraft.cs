using ErrorOr;
using Mediator;

namespace EmbassyAirlines.Application.Commands;

public sealed record DeleteAircraft(Guid Id): ICommand<ErrorOr<int>>;