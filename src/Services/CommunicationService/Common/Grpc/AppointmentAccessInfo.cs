namespace CommunicationService.Common.Grpc;

public sealed class AppointmentAccessInfo
{
    public Guid AppointmentId { get; init; }
    public Guid PatientId { get; init; }
    public Guid DoctorId { get; init; }
    public string PatientKeycloakId { get; init; } = string.Empty;
    public string DoctorKeycloakId { get; init; } = string.Empty;
    public string PatientName { get; init; } = string.Empty;
    public string DoctorName { get; init; } = string.Empty;
}
