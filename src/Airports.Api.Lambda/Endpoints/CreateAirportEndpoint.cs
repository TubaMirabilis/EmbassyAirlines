using Airports.Core.Models;
using Airports.Infrastructure.Database;
using ErrorOr;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;
using Shared.Extensions;

namespace Airports.Api.Lambda.Endpoints;

internal sealed class CreateAirportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("airports", InvokeAsync)
              .WithSummary("Create an airport")
              .Accepts<CreateOrUpdateAirportDto>("application/json")
              .Produces<AirportDto>(StatusCodes.Status201Created)
              .ProducesProblem(StatusCodes.Status400BadRequest)
              .ProducesProblem(StatusCodes.Status409Conflict)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private static async Task<Results<Created<AirportDto>, ProblemHttpResult>> InvokeAsync(ApplicationDbContext ctx,
                                                                                           ILogger<CreateAirportEndpoint> logger,
                                                                                           IValidator<CreateOrUpdateAirportDto> validator,
                                                                                           TimeProvider timeProvider,
                                                                                           CreateOrUpdateAirportDto dto,
                                                                                           CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid(out var formattedErrors))
        {
            logger.LogWarning("Validation failed for creation of airport: {Errors}", formattedErrors);
            var error = Error.Validation("Airport.Validation", formattedErrors);
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        if (await ctx.Airports.AsNoTracking().AnyAsync(a => a.IataCode == dto.IataCode, ct))
        {
            logger.LogWarning("Conflict: Airport with IATA code {IataCode} already exists", dto.IataCode);
            var error = Error.Conflict("Airport.Conflict", $"Airport with IATA code {dto.IataCode} already exists");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        var airport = Airport.Create(dto.IcaoCode, dto.IataCode, dto.Name, dto.TimeZoneId, timeProvider.GetUtcNow());
        ctx.Airports.Add(airport);
        await ctx.SaveChangesAsync(ct);
        var body = new AirportDto(airport.Id, airport.Name, airport.IcaoCode, airport.IataCode, airport.TimeZoneId);
        return TypedResults.Created($"/airports/{airport.Id}", body);
    }
}
