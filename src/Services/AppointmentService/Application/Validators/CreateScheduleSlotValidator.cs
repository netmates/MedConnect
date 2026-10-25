using AppointmentService.Application.DTOs.ScheduleSlot;
using FluentValidation;

namespace AppointmentService.Application.Validators;

public sealed class CreateScheduleSlotValidator : AbstractValidator<CreateScheduleSlotDto>
{
    public CreateScheduleSlotValidator() =>
        Include(new ScheduleSlotTimeValidator<CreateScheduleSlotDto>());
}
