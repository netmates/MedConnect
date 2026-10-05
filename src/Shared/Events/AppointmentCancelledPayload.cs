namespace MedConnect.Shared.Events;

public sealed class AppointmentCancelledPayload
{
    public Guid AppointmentId { get; init; }
    public Guid PatientId { get; init; }
    public Guid DoctorId { get; init; }
    public Guid SlotId { get; init; }
    public Guid CancelledByUserId { get; init; }
    public string CancelledByRole { get; init; } = string.Empty;
    public string? CancelReason { get; init; }
    public DateTime CancelledAt { get; init; }
}
