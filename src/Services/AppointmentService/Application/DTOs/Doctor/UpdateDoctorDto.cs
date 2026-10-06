namespace AppointmentService.Application.DTOs.Doctor;

public sealed record UpdateDoctorDto
{
    public string LastName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string Description { get; init; } = string.Empty;
    public int ExperienceYears { get; init; }
    public List<Guid> SpecializationIds { get; init; } = [];
}
