using MongoDB.Bson.Serialization.Attributes;

namespace CommunicationService.Common.Persistence;

public sealed class ChatDocument
{
    [BsonId]
    public Guid Id { get; init; }
    public Guid AppointmentId { get; init; }
    public Guid PatientId { get; init; }
    public Guid DoctorId { get; init; }
    public string PatientKeycloakId { get; init; } = null!;
    public string DoctorKeycloakId { get; init; } = null!;
    public string PatientName { get; private set; } = null!;
    public string DoctorName { get; private set; } = null!;
    public DateTime CreatedAt { get; init; }

    public static ChatDocument Create(
        Guid appointmentId,
        Guid patientId,
        Guid doctorId,
        string patientKeycloakId,
        string doctorKeycloakId,
        string patientName,
        string doctorName)
    {
        return new ChatDocument
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            PatientId = patientId,
            DoctorId = doctorId,
            PatientKeycloakId = patientKeycloakId,
            DoctorKeycloakId = doctorKeycloakId,
            PatientName = patientName.Trim(),
            DoctorName = doctorName.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdatePatientName(string patientName)
    {
        PatientName = patientName.Trim();
    }

    public void UpdateDoctorName(string doctorName)
    {
        DoctorName = doctorName.Trim();
    }
}
