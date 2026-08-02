using AppointmentService.Application.DTOs.ScheduleSlot;

namespace AppointmentService.Application.Interfaces.Services;

public interface IScheduleSlotApplicationService
{
    /// <summary>
    /// Создать слот расписания.
    /// </summary>
    Task<ScheduleSlotDto> CreateAsync(CreateScheduleSlotDto dto, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Обновить слот.
    /// </summary>
    Task<ScheduleSlotDto> UpdateAsync(Guid id, UpdateScheduleSlotDto dto, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Удалить слот.
    /// </summary>
    Task DeleteAsync(Guid id, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Получить список слотов врача.
    /// </summary>
    Task<IReadOnlyList<ScheduleSlotDto>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct);
    /// <summary>
    /// Получить свободные слоты врача на указанную дату.
    /// </summary>
    Task<IReadOnlyList<ScheduleSlotDto>> GetAvailableAsync(Guid doctorId, DateTime date, CancellationToken ct);
}
