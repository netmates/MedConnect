namespace AppointmentService.Domain.Entities;

public class Doctor
{
    public Guid Id { get; private set; }
    public string KeycloakId { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string? MiddleName { get; private set; }
    public string Description { get; private set; } = null!;
    public int ExperienceYears { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ICollection<DoctorSpecialization> DoctorSpecializations { get; private set; } = new List<DoctorSpecialization>();
    public ICollection<ScheduleSlot> ScheduleSlots { get; private set; } = new List<ScheduleSlot>();

    private Doctor() { }

    public static Doctor Create(
        string keycloakId,
        string lastName,
        string firstName,
        string? middleName,
        string description,
        int experienceYears)
    {
        return new Doctor
        {
            Id = Guid.NewGuid(),
            KeycloakId = keycloakId,
            LastName = lastName,
            FirstName = firstName,
            MiddleName = middleName,
            Description = description,
            ExperienceYears = experienceYears,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string lastName,
        string firstName,
        string? middleName,
        string description,
        int experienceYears)
    {
        LastName = lastName;
        FirstName = firstName;
        MiddleName = middleName;
        Description = description;
        ExperienceYears = experienceYears;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
