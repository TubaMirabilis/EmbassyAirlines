using EaCommon.Errors;
using EmbassyAirlines.Application.Commands;
using EmbassyAirlines.Application.Repositories;
using FluentResults;
using Mediator;

namespace EmbassyAirlines.Application.CommandHandlers;

public sealed class DeleteAircraftCommandHandler : ICommandHandler<DeleteAircraft, Result<Unit>>
{
    private readonly IFleetRepository _repository;
    public DeleteAircraftCommandHandler(IFleetRepository repository)
    {
        _repository = repository;
    }
    public async ValueTask<Result<Unit>> Handle(DeleteAircraft command, CancellationToken ct)
    {
        var rowsAffected = await _repository.DeleteAircraftAsync(command.Id, ct);
        if (rowsAffected == 0)
        {
            return Result.Fail(new NotFoundError("Aircraft"));
        }
        return Unit.Value;
    }
}