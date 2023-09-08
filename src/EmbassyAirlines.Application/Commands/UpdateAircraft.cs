using EmbassyAirlines.Application.Dtos;

namespace EmbassyAirlines.Application.Commands;

public sealed record UpdateAircraft(Guid Id, UpdateAircraftDto dto);