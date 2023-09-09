using System.Data;
using EmbassyAirlines.Application.Commands;
using FluentValidation;

namespace EmbassyAirlines.Application.Validators;

public class UpdateAircraftValidator : AbstractValidator<UpdateAircraft>
{
    public UpdateAircraftValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Dto)
            .NotNull().SetValidator(new UpdateAircraftDtoValidator());
    }
}