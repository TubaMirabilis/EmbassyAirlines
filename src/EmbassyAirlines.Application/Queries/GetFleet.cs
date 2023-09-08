using EmbassyAirlines.Application.Dtos;
using Mediator;

namespace EmbassyAirlines.Application.Queries;

public sealed record GetFleet() : IQuery<IEnumerable<AircraftDto>>;