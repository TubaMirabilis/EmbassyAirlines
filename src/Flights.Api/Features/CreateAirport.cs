using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Domain.Airports;
using Flights.Api.Extensions;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Features;

public static class CreateAirport
{
    public sealed record Command(CreateAirportDto Dto) : ICommand<ErrorOr<AirportDto>>;
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Dto.IataCode)
                .Matches("^[A-Z]{3}$")
                    .WithMessage("IATA Code must consist of 3 uppercase letters only.");
            RuleFor(x => x.Dto.Name)
                .NotEmpty()
                    .WithMessage("Name is required.")
                .MaximumLength(100)
                    .WithMessage("Name must not exceed 100 characters in length.");
            RuleFor(x => x.Dto.TimeZoneId)
                .NotEmpty()
                    .WithMessage("Time zone is required.")
                .MaximumLength(100)
                    .WithMessage("Time zone must not exceed 100 characters in length.");
        }
    }
    public sealed class Handler : ICommandHandler<Command, ErrorOr<AirportDto>>
    {
        private readonly ApplicationDbContext _ctx;
        private readonly ILogger<Handler> _logger;
        private readonly IValidator<Command> _validator;
        public Handler(ApplicationDbContext ctx, ILogger<Handler> logger, IValidator<Command> validator)
        {
            _ctx = ctx;
            _logger = logger;
            _validator = validator;
        }
        public async ValueTask<ErrorOr<AirportDto>> Handle(Command command, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid(out var formattedErrors))
            {
                _logger.LogWarning("Validation failed for {Command}. Errors: {Errors}", command, formattedErrors);
                return Error.Validation("Command.ValidationFailed", formattedErrors);
            }
            var airportsWithSameIataCode = await _ctx.Airports.CountAsync(x => x.IataCode == command.Dto.IataCode, cancellationToken);
            if (airportsWithSameIataCode > 0)
            {
                _logger.LogWarning("Airport with IATA code {IataCode} already exists", command.Dto.IataCode);
                return Error.Conflict("Airport.Conflict", $"Airport with IATA code {command.Dto.IataCode} already exists");
            }
            var airport = Airport.Create(command.Dto.IataCode, command.Dto.Name, command.Dto.TimeZoneId);
            _ctx.Airports
                .Add(airport);
            await _ctx.SaveChangesAsync(cancellationToken);
            return new AirportDto(airport.Id, airport.Name, airport.IataCode, airport.TimeZoneId);
        }
    }
}
public sealed class CreateAirportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("airports", CreateAirport)
              .WithName("createAirport")
              .Produces(StatusCodes.Status201Created)
              .WithOpenApi();
    private static async Task<IResult> CreateAirport([FromServices] ISender sender, [FromBody] CreateAirportDto dto, CancellationToken ct)
    {
        var command = new CreateAirport.Command(dto);
        var result = await sender.Send(command, ct);
        return result.Match(
            res => Results.Created($"airports/{res.Id}", res),
            ErrorHandlingHelper.HandleProblems);
    }
}
