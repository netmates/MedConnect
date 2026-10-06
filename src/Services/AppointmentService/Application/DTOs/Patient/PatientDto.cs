namespace AppointmentService.Application.DTOs.Patient;

public sealed record PatientDto
{
    public Guid Id { get; init; }
    public string KeycloakId { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string? Phone { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
