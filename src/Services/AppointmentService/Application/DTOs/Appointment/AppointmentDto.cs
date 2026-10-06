namespace AppointmentService.Application.DTOs.Appointment;

public sealed record AppointmentDto
{
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public Guid DoctorId { get; init; }
    public Guid SlotId { get; init; }
    public string? Reason { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string DoctorFullName { get; init; } = string.Empty;
    public string PatientFullName { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
}
