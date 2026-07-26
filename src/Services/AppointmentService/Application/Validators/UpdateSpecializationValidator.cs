using AppointmentService.Application.DTOs.Specialization;
using AppointmentService.Domain.Entities;
using FluentValidation;

namespace AppointmentService.Application.Validators;

public class UpdateSpecializationValidator : AbstractValidator<UpdateSpecializationDto>
{
    public UpdateSpecializationValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Название специализации обязательно.")
            .MaximumLength(Specialization.MaxNameLength)
                .WithMessage($"Название специализации не должно превышать {Specialization.MaxNameLength} символов.");
    }
}
