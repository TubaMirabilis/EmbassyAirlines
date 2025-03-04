namespace Shared.Contracts;

public sealed record JourneyListDto(IEnumerable<IEnumerable<FlightDto>> Journeys);
