using EmbassyAirlines.Application.Commands;
using EmbassyAirlines.Application.Repositories;
using EmbassyAirlines.Domain.DomainErrors;
using ErrorOr;
using Mediator;

namespace EmbassyAirlines.Application.CommandHandlers;

public sealed class DeleteAircraftCommandHandler : ICommandHandler<DeleteAircraft, ErrorOr<int>>
{
    private readonly IFleetRepository _repository;
    public DeleteAircraftCommandHandler(IFleetRepository repository)
    {
        _repository = repository;
    }
    public async ValueTask<ErrorOr<int>> Handle(DeleteAircraft command, CancellationToken ct)
    {
        var rowsAffected = await _repository.DeleteAircraftAsync(command.Id, ct);
        if (rowsAffected == 0)
        {
            return Errors.Aircraft.NotFound;
        }       
        return rowsAffected;
    }
}