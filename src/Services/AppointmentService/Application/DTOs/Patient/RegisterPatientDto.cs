namespace AppointmentService.Application.DTOs.Patient;

public class RegisterPatientDto
{
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
}
