namespace AppointmentService.Application.DTOs.Doctor;

public class DoctorDto
{
    public Guid Id { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Description { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public bool IsActive { get; set; }
    public List<string> Specializations { get; set; } = [];
}
