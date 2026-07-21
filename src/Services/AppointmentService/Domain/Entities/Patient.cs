namespace AppointmentService.Domain.Entities;

public class Patient
{
    public Guid Id { get; private set; }
    public string KeycloakId { get; private set; }
    public string LastName { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string? MiddleName { get; private set; }
    public string? Phone { get; private set; }
    public DateTime? DateOfBirth { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ICollection<Appointment> Appointments { get; private set; } = new List<Appointment>();

    private Patient() { }

    public static Patient Create(
        string keycloakId,
        string lastName,
        string firstName,
        string? middleName)
    {
        return new Patient
        {
            Id = Guid.NewGuid(),
            KeycloakId = keycloakId,
            LastName = lastName,
            FirstName = firstName,
            MiddleName = middleName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(
        string lastName,
        string firstName,
        string? middleName,
        string? phone,
        DateTime? dateOfBirth)
    {
        LastName = lastName;
        FirstName = firstName;
        MiddleName = middleName;
        Phone = phone;
        DateOfBirth = dateOfBirth;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
