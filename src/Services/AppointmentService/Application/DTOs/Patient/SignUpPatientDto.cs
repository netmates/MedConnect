namespace AppointmentService.Application.DTOs.Patient;

public sealed record SignUpPatientDto : IPatientProfile
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string? Phone { get; init; }
    public DateTime? DateOfBirth { get; init; }
}
