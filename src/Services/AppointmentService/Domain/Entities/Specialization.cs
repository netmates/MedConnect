namespace AppointmentService.Domain.Entities;

public class Specialization
{
    public const int MaxNameLength = 200;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;

    public ICollection<DoctorSpecialization> DoctorSpecializations { get; private set; } = new List<DoctorSpecialization>();

    private Specialization() { }

    public static Specialization Create(string name) => new() { Id = Guid.NewGuid(), Name = name };
        
    public void Update(string name) => Name = name;
}
