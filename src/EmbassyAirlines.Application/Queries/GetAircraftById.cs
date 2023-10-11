using EmbassyAirlines.Application.Dtos;
using FluentResults;
using Mediator;

namespace EmbassyAirlines.Application.Queries;

public sealed record GetAircraftById(Guid Id) : IQuery<Result<AircraftDto>>;