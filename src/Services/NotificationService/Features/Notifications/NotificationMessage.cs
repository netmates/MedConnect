namespace NotificationService.Features.Notifications;

public sealed record NotificationMessage
{
    public required string EventType { get; init; }
    public required Guid RecipientId { get; init; }
    public required string RecipientRole { get; init; }
    public required string TextPreview { get; init; }
    public Guid? AppointmentId { get; init; }
    public Guid? PatientId { get; init; }
    public Guid? DoctorId { get; init; }
    public Guid? MessageId { get; init; }
}
