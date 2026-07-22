namespace AppointmentService.Application.DTOs.Doctor;

public class DoctorListItemDto
{
    public Guid Id { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public int ExperienceYears { get; set; }
    public List<string> Specializations { get; set; } = [];
}
