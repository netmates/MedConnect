namespace AppointmentService.Application.DTOs.Doctor;

public class CreateDoctorDto
{
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public List<Guid> SpecializationIds { get; set; } = [];
}
