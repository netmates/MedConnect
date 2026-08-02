using AppointmentService.Domain.Exceptions;

namespace AppointmentService.Domain.Entities;

public class DoctorSpecialization
{
    public Guid DoctorId { get; private set; }
    public Guid SpecializationId { get; private set; }

    public Doctor Doctor { get; private set; } = null!;
    public Specialization Specialization { get; private set; } = null!;

    private DoctorSpecialization() { }

    public static DoctorSpecialization Create(Guid doctorId, Guid specializationId)
    {
        if (doctorId == Guid.Empty)
            throw new DomainException("DoctorId обязателен.");

        if (specializationId == Guid.Empty)
            throw new DomainException("SpecializationId обязателен.");

        return new DoctorSpecialization
        {
            DoctorId = doctorId,
            SpecializationId = specializationId
        };
    }
}
