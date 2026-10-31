using AppointmentService.Application.DTOs.Specialization;

namespace AppointmentService.Application.Interfaces.Caching;

public interface ISpecializationListCache
{
    public Task<IReadOnlyList<SpecializationDto>?> GetAsync(CancellationToken ct);
    public Task SetAsync(IReadOnlyList<SpecializationDto> items, CancellationToken ct);
    public Task RemoveAsync(CancellationToken ct);
}
