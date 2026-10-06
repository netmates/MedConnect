namespace AppointmentService.Application.DTOs.Doctor;

public sealed record DoctorDto
{
    public Guid Id { get; init; }
    public string KeycloakId { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string Description { get; init; } = string.Empty;
    public int ExperienceYears { get; init; }
    public bool IsActive { get; init; }
    public List<string> Specializations { get; init; } = [];
}
