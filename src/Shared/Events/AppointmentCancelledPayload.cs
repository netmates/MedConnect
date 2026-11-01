namespace MedConnect.Shared.Events;

public sealed class AppointmentCancelledPayload
{
    public Guid AppointmentId { get; init; }
    public Guid PatientId { get; init; }
    public Guid DoctorId { get; init; }
    public Guid SlotId { get; init; }
    public Guid CancelledByUserId { get; init; }
    public string CancelledByRole { get; init; } = string.Empty;
    public DateTime CancelledAt { get; init; }
    public DateTime StartTime { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string DoctorName { get; init; } = string.Empty;
}
