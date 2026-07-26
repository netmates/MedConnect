using AppointmentService.Domain.Exceptions;

namespace AppointmentService.Domain.Entities;

public class Doctor
{
    public const int MaxKeycloakIdLength = 255;
    public const int MaxLastNameLength = 100;
    public const int MaxFirstNameLength = 100;
    public const int MaxMiddleNameLength = 100;
    public const int MaxDescriptionLength = 1000;
    public const int MinExperienceYears = 0;
    public const int MaxExperienceYears = 70;

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
        EnsureKeycloakId(keycloakId);
        EnsureProfile(lastName, firstName, middleName, description, experienceYears);

        return new Doctor
        {
            Id = Guid.NewGuid(),
            KeycloakId = keycloakId.Trim(),
            LastName = lastName.Trim(),
            FirstName = firstName.Trim(),
            MiddleName = NormalizeOptional(middleName),
            Description = description.Trim(),
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
        EnsureProfile(lastName, firstName, middleName, description, experienceYears);

        LastName = lastName.Trim();
        FirstName = firstName.Trim();
        MiddleName = NormalizeOptional(middleName);
        Description = description.Trim();
        ExperienceYears = experienceYears;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    private static void EnsureKeycloakId(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            throw new DomainException("KeycloakId обязателен.");

        if (keycloakId.Trim().Length > MaxKeycloakIdLength)
            throw new DomainException($"KeycloakId не должен превышать {MaxKeycloakIdLength} символов.");
    }

    private static void EnsureProfile(
        string lastName,
        string firstName,
        string? middleName,
        string description,
        int experienceYears)
    {
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Фамилия обязательна.");

        if (lastName.Trim().Length > MaxLastNameLength)
            throw new DomainException($"Фамилия не должна превышать {MaxLastNameLength} символов.");

        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("Имя обязательно.");

        if (firstName.Trim().Length > MaxFirstNameLength)
            throw new DomainException($"Имя не должно превышать {MaxFirstNameLength} символов.");

        if (!string.IsNullOrWhiteSpace(middleName) && middleName.Trim().Length > MaxMiddleNameLength)
            throw new DomainException($"Отчество не должно превышать {MaxMiddleNameLength} символов.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Описание обязательно.");

        if (description.Trim().Length > MaxDescriptionLength)
            throw new DomainException($"Описание не должно превышать {MaxDescriptionLength} символов.");

        if (experienceYears < MinExperienceYears || experienceYears > MaxExperienceYears)
            throw new DomainException(
                $"Опыт должен быть от {MinExperienceYears} до {MaxExperienceYears} лет.");
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
