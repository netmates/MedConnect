namespace AppointmentService.Application.DTOs.Patient;

public sealed record RegisterPatientDto
{
    public string LastName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string? Phone { get; init; }
    public DateTime? DateOfBirth { get; init; }
}
