using AppointmentService.Application.DTOs.ScheduleSlot;

namespace AppointmentService.Application.Interfaces.Caching;

public interface IDoctorAvailableSlotCache
{
    public Task<IReadOnlyList<ScheduleSlotDto>?> GetAsync(Guid doctorId, DateTime date, CancellationToken ct);
    public Task SetAsync(Guid doctorId, DateTime date, IReadOnlyList<ScheduleSlotDto> slots, CancellationToken ct);
    public Task RemoveAsync(Guid doctorId, DateTime date, CancellationToken ct);
}
