namespace AppointmentService.Application.DTOs.ScheduleSlot;

public sealed record UpdateScheduleSlotDto
{
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
}
