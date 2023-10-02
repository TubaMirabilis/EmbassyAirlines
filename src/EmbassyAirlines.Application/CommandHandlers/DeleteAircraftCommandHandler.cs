using EmbassyAirlines.Application.Commands;
using EmbassyAirlines.Application.Exceptions;
using EmbassyAirlines.Application.Repositories;
using Mediator;

namespace EmbassyAirlines.Application.CommandHandlers;

public sealed class DeleteAircraftCommandHandler : ICommandHandler<DeleteAircraft>
{
    private readonly IFleetRepository _repository;
    public DeleteAircraftCommandHandler(IFleetRepository repository)
    {
        _repository = repository;
    }
    public async ValueTask<Unit> Handle(DeleteAircraft command, CancellationToken ct)
    {
        var rowsAffected = await _repository.DeleteAircraftAsync(command.Id, ct);
        if (rowsAffected == 0)
        {
            throw new NotFoundException($"Aircraft with id {command.Id} not found");
        }       
        return Unit.Value;
    }
}