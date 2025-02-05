using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Extensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Features;

public static class RetrieveItinerary
{
    public sealed record Query(string Slug) : IQuery<ErrorOr<ItineraryDto>>;
    public sealed class Handler : IQueryHandler<Query, ErrorOr<ItineraryDto>>
    {
        private readonly ApplicationDbContext _ctx;
        public Handler(ApplicationDbContext ctx) => _ctx = ctx;
        public async ValueTask<ErrorOr<ItineraryDto>> Handle(Query query, CancellationToken cancellationToken)
        {
            var itinerary = await _ctx.Itineraries
                                      .AsNoTracking()
                                      .SingleOrDefaultAsync(i => i.Reference == query.Slug, cancellationToken);
            return itinerary is not null
                ? itinerary.ToDto()
                : Error.NotFound("Query.NotFound", "Itinerary not found.");
        }
    }
}
public sealed class RetrieveItineraryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("itineraries/{slug}", RetrieveItinerary)
              .WithName("retrieveItinerary")
              .WithOpenApi();
    private static async Task<IResult> RetrieveItinerary([FromServices] ISender sender, [FromRoute] string slug, CancellationToken ct)
    {
        var query = new RetrieveItinerary.Query(slug);
        var result = await sender.Send(query, ct);
        return result.Match(
            Results.Ok,
            ErrorHandlingHelper.HandleProblems);
    }
}
