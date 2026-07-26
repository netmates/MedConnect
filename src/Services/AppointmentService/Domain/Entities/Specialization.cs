using AppointmentService.Domain.Exceptions;

namespace AppointmentService.Domain.Entities;

public class Specialization
{
    public const int MaxNameLength = 200;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;

    public ICollection<DoctorSpecialization> DoctorSpecializations { get; private set; } = new List<DoctorSpecialization>();

    private Specialization() { }

    public static Specialization Create(string name)
    {
        EnsureName(name);

        return new Specialization { Id = Guid.NewGuid(), Name = name.Trim() };
    }

    public void Update(string name)
    {
        EnsureName(name);

        Name = name.Trim();
    }

    private static void EnsureName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Название специализации обязательно.");

        if (name.Trim().Length > MaxNameLength)
            throw new DomainException($"Название не должно превышать {MaxNameLength} символов.");
    }
}
