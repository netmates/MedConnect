namespace AppointmentService.Application.DTOs.Doctor;

public sealed record CreateDoctorDto
{
    public string LastName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string Email { get; init; } = string.Empty;
    public string TemporaryPassword { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int ExperienceYears { get; init; }
    public List<Guid> SpecializationIds { get; init; } = [];
}
