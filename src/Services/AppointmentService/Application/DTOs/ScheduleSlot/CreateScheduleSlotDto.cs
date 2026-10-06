namespace AppointmentService.Application.DTOs.ScheduleSlot;

public sealed record CreateScheduleSlotDto
{
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
}
