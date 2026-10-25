namespace AppointmentService.Application.DTOs.ScheduleSlot;

public sealed record UpdateScheduleSlotDto : IScheduleSlotTime
{
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
}
