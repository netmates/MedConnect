using AppointmentService.Application.DTOs.Doctor;

namespace AppointmentService.Application.Interfaces.Caching;

public interface IDoctorCardCache
{
    public Task<DoctorDto?> GetAsync(Guid doctorId, CancellationToken ct);
    public Task SetAsync(DoctorDto doctor, CancellationToken ct);
    public Task RemoveAsync(Guid doctorId, CancellationToken ct);
}
