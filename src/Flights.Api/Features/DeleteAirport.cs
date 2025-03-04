using ErrorOr;
using Flights.Api.Database;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Flights.Api.Features;

public static class DeleteAirport
{
    public sealed record Command(Guid Id) : ICommand<ErrorOr<Unit>>;
    public sealed class Handler : ICommandHandler<Command, ErrorOr<Unit>>
    {
        private readonly ApplicationDbContext _ctx;
        public Handler(ApplicationDbContext ctx) => _ctx = ctx;
        public async ValueTask<ErrorOr<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var rowsAffected = await _ctx.Airports.Where(a => a.Id == command.Id).ExecuteDeleteAsync(cancellationToken);
            return rowsAffected == 0
                ? Error.NotFound("Airport.NotFound", "Airport not found.")
                : Unit.Value;
        }
    }
}
public sealed class DeleteAirportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapDelete("airports/{id}", DeleteAirport)
              .WithName("deleteAirport")
              .WithOpenApi();
    private static async Task<IResult> DeleteAirport([FromServices] ISender sender, [FromRoute] Guid id, CancellationToken ct)
    {
        var command = new DeleteAirport.Command(id);
        var result = await sender.Send(command, ct);
        return result.Match(
            _ => Results.NoContent(),
            ErrorHandlingHelper.HandleProblems);
    }
}
