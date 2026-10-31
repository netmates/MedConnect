using AppointmentService.Application.DTOs.Doctor;

namespace AppointmentService.Application.Interfaces.Caching;

public interface IDoctorListCache
{
    public Task<IReadOnlyList<DoctorListEntry>?> GetAsync(CancellationToken ct);
    public Task SetAsync(IReadOnlyList<DoctorListEntry> items, CancellationToken ct);
    public Task RemoveAsync(CancellationToken ct);
}

public sealed class DoctorListEntry
{
    public required DoctorDto Doctor { get; init; }
    public List<Guid> SpecializationIds { get; init; } = [];
}
