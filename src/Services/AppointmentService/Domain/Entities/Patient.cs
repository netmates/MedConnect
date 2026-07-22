namespace AppointmentService.Domain.Entities;

public class Patient
{
    public const int MaxKeycloakIdLength = 255;
    public const int MaxLastNameLength = 100;
    public const int MaxFirstNameLength = 100;
    public const int MaxMiddleNameLength = 100;
    public const int MinPhoneLength = 7;
    public const int MaxPhoneLength = 20;

    public const string PhoneRegexPattern = @"^\+?[0-9\s\-]{7,20}$";

    public Guid Id { get; private set; }
    public string KeycloakId { get; private set; } = null!;
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
