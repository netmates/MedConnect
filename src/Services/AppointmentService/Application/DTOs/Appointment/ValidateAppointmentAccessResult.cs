namespace AppointmentService.Application.DTOs.Appointment;

public enum AppointmentAccessDenial
{
    None = 0,
    NotFound = 1,
    Forbidden = 2,
    Closed = 3
}

public sealed class ValidateAppointmentAccessResult
{
    public bool Allowed { get; init; }
    public AppointmentAccessDenial DenialReason { get; init; }
    public Guid AppointmentId { get; init; }
    public Guid PatientId { get; init; }
    public Guid DoctorId { get; init; }
    public string PatientKeycloakId { get; init; } = string.Empty;
    public string DoctorKeycloakId { get; init; } = string.Empty;
    public string PatientName { get; init; } = string.Empty;
    public string DoctorName { get; init; } = string.Empty;

    public static ValidateAppointmentAccessResult Allow(
        Guid appointmentId,
        Guid patientId,
        Guid doctorId,
        string patientKeycloakId,
        string doctorKeycloakId,
        string patientName,
        string doctorName)
    {
        return new ValidateAppointmentAccessResult
        {
            Allowed = true,
            DenialReason = AppointmentAccessDenial.None,
            AppointmentId = appointmentId,
            PatientId = patientId,
            DoctorId = doctorId,
            PatientKeycloakId = patientKeycloakId,
            DoctorKeycloakId = doctorKeycloakId,
            PatientName = patientName,
            DoctorName = doctorName
        };
    }

    public static ValidateAppointmentAccessResult Deny(AppointmentAccessDenial denial) => new()
    {
        Allowed = false,
        DenialReason = denial
    };
}
