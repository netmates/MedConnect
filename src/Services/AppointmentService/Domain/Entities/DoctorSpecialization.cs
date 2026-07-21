namespace AppointmentService.Domain.Entities;

public class DoctorSpecialization
{
    public Guid DoctorId { get; set; }
    public Guid SpecializationId { get; set; }

    public Doctor Doctor { get; set; } = null!;
    public Specialization Specialization { get; set; } = null!;
}
