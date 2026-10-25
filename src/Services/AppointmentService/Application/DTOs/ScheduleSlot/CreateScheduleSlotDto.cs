namespace AppointmentService.Application.DTOs.ScheduleSlot;

public sealed record CreateScheduleSlotDto : IScheduleSlotTime
{
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
}
