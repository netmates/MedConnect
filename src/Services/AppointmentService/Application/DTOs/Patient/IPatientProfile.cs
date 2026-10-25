namespace AppointmentService.Application.DTOs.Patient;

public interface IPatientProfile
{
    public string LastName { get; }
    public string FirstName { get; }
    public string? MiddleName { get; }
    public string? Phone { get; }
    public DateTime? DateOfBirth { get; }
}
