namespace MedConnect.Shared.Events;

public sealed class AppointmentCreatedPayload
{
    public Guid AppointmentId { get; init; }
    public Guid PatientId { get; init; }
    public Guid DoctorId { get; init; }
    public Guid SlotId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string? Reason { get; init; }

    public string PatientKeycloakId { get; init; } = string.Empty;
    public string DoctorKeycloakId { get; init; } = string.Empty;
    public string PatientName { get; init; } = string.Empty;
    public string DoctorName { get; init; } = string.Empty;
}