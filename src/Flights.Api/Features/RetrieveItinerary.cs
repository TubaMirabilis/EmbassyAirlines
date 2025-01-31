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
    public sealed record Query(Guid Id) : IQuery<ErrorOr<ItineraryDto>>;
    public sealed class Handler : IQueryHandler<Query, ErrorOr<ItineraryDto>>
    {
        private readonly ApplicationDbContext _ctx;
        public Handler(ApplicationDbContext ctx) => _ctx = ctx;
        public async ValueTask<ErrorOr<ItineraryDto>> Handle(Query query, CancellationToken cancellationToken)
        {
            var itinerary = await _ctx.Itineraries
                                      .AsNoTracking()
                                      .SingleOrDefaultAsync(i => i.Id == query.Id, cancellationToken);
            return itinerary is not null
                ? itinerary.ToDto()
                : Error.NotFound("Query.NotFound", "Itinerary not found");
        }
    }
    public sealed class RetrieveItineraryEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
            => app.MapGet("itineraries/{slug}", RetrieveItinerary)
                  .WithName("RetrieveItinerary")
                  .WithOpenApi();
        private static async Task<IResult> RetrieveItinerary([FromServices] ISender sender, [FromRoute] Guid slug, CancellationToken ct)
        {
            var query = new Query(slug);
            var result = await sender.Send(query, ct);
            return result.Match(
                Results.Ok,
                ErrorHandlingHelper.HandleProblems);
        }
    }
}
