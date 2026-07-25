using AppointmentService.Application.DTOs.Appointment;
using AppointmentService.Domain.Entities;
using FluentValidation;

namespace AppointmentService.Application.Validators;

public class CreateAppointmentValidator : AbstractValidator<CreateAppointmentDto>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.SlotId)
            .NotEmpty()
            .WithMessage("SlotId обязателен.");

        RuleFor(x => x.Reason)
            .MaximumLength(Appointment.MaxReasonLength)
            .WithMessage($"Причина не должна превышать {Appointment.MaxReasonLength} символов.");
    }
}
