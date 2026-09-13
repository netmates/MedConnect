namespace AppointmentService.Application.Helpers;

public static class FullNameFormatter
{
    public static string Format(string lastName, string firstName, string? middleName)
        => string.IsNullOrWhiteSpace(middleName)
            ? $"{lastName} {firstName}".Trim()
            : $"{lastName} {firstName} {middleName}".Trim();
}
