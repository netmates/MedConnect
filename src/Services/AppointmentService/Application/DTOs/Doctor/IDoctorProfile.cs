namespace AppointmentService.Application.DTOs.Doctor;

public interface IDoctorProfile
{
    public string LastName { get; }
    public string FirstName { get; }
    public string? MiddleName { get; }
    public string Description { get; }
    public int ExperienceYears { get; }
    public List<Guid> SpecializationIds { get; }
}
