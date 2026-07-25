using AppointmentService.Application.DTOs.Slot;
using AppointmentService.Domain.Entities;
using FluentValidation;

namespace AppointmentService.Application.Validators;

public class UpdateSlotValidator : AbstractValidator<UpdateSlotDto>
{
    public UpdateSlotValidator()
    {
        RuleFor(x => x.StartTime)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Время начала должно быть в будущем.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("Время окончания должно быть позже времени начала.");

        RuleFor(x => x)
            .Must(x => (x.EndTime - x.StartTime).TotalMinutes
                is >= ScheduleSlot.MinDurationMinutes
                and <= ScheduleSlot.MaxDurationMinutes)
            .WithMessage($"Длительность слота должна быть " +
                 $"от {ScheduleSlot.MinDurationMinutes} " +
                 $"до {ScheduleSlot.MaxDurationMinutes} минут.")
            .When(x => x.EndTime > x.StartTime);
    }
}
