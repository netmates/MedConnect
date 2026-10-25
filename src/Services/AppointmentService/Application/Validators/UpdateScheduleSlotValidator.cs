using AppointmentService.Application.DTOs.ScheduleSlot;
using FluentValidation;

namespace AppointmentService.Application.Validators;

public sealed class UpdateScheduleSlotValidator : AbstractValidator<UpdateScheduleSlotDto>
{
    public UpdateScheduleSlotValidator() =>
        Include(new ScheduleSlotTimeValidator<UpdateScheduleSlotDto>());
}
