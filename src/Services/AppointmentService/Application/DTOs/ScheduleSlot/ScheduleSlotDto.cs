namespace AppointmentService.Application.DTOs.ScheduleSlot;

public sealed record ScheduleSlotDto
{
    public Guid Id { get; init; }
    public Guid DoctorId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
