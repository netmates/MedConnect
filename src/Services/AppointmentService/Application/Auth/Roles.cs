namespace AppointmentService.Application.Auth;

/// <summary>
/// Имена realm-ролей Keycloak. Значения должны совпадать с ролями в IdP.
/// </summary>
public static class Roles
{
    public const string Admin = "admin";
    public const string Doctor = "doctor";
    public const string Patient = "patient";

    /// <summary>Для [Authorize(Roles = ...)] — несколько ролей через запятую.</summary>
    public const string PatientOrDoctor = Patient + "," + Doctor;
}
