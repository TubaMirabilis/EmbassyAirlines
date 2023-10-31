using EmbassyAirlines.Application.Dtos;
using ErrorOr;
using Mediator;

namespace EmbassyAirlines.Application.Queries;

public sealed record GetAircraftById(Guid Id) : IQuery<ErrorOr<AircraftDto>>;