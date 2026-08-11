namespace CommunicationService.Features.Chats;

public sealed record CreateChatRequest(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    string PatientKeycloakId,
    string DoctorKeycloakId,
    string PatientName,
    string DoctorName);
