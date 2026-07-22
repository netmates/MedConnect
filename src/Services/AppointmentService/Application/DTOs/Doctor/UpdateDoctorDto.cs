namespace AppointmentService.Application.DTOs.Doctor;

public class UpdateDoctorDto
{
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Description { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
}
