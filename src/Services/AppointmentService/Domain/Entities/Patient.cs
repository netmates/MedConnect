using AppointmentService.Domain.Exceptions;
using System.Text.RegularExpressions;

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

    public static readonly DateTime MinDateOfBirth = new(1900, 1, 1);

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
        string? middleName,
        string? phone,
        DateTime? dateOfBirth)
    {
        EnsureKeycloakId(keycloakId);
        EnsureProfile(lastName, firstName, middleName, phone, dateOfBirth);

        return new Patient
        {
            Id = Guid.NewGuid(),
            KeycloakId = keycloakId.Trim(),
            LastName = lastName.Trim(),
            FirstName = firstName.Trim(),
            MiddleName = NormalizeOptional(middleName),
            Phone = NormalizeOptional(phone),
            DateOfBirth = dateOfBirth,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string lastName,
        string firstName,
        string? middleName,
        string? phone,
        DateTime? dateOfBirth)
    {
        EnsureProfile(lastName, firstName, middleName, phone, dateOfBirth);

        LastName = lastName.Trim();
        FirstName = firstName.Trim();
        MiddleName = NormalizeOptional(middleName);
        Phone = NormalizeOptional(phone);
        DateOfBirth = dateOfBirth;
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
        string? phone,
        DateTime? dateOfBirth)
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

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var normalizedPhone = phone.Trim();
            if (normalizedPhone.Length < MinPhoneLength || normalizedPhone.Length > MaxPhoneLength)
                throw new DomainException(
                    $"Телефон должен содержать от {MinPhoneLength} до {MaxPhoneLength} символов.");

            if (!Regex.IsMatch(normalizedPhone, PhoneRegexPattern))
                throw new DomainException("Некорректный формат телефона.");
        }

        if (dateOfBirth.HasValue)
        {
            if (dateOfBirth.Value.Date < MinDateOfBirth.Date)
                throw new DomainException($"Дата рождения не может быть раньше {MinDateOfBirth:yyyy-MM-dd}.");
            if (dateOfBirth.Value.Date >= DateTime.UtcNow.Date)
                throw new DomainException("Дата рождения должна быть в прошлом.");
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
