namespace CommunicationService.Common.Auth;

public static class Roles
{
    public const string Doctor = "doctor";
    public const string Patient = "patient";

    public const string PatientOrDoctor = Patient + "," + Doctor;
}
