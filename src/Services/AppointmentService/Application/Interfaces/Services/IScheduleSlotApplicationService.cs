using AppointmentService.Application.DTOs.ScheduleSlot;

namespace AppointmentService.Application.Interfaces.Services;

public interface IScheduleSlotApplicationService
{
    /// <summary>
    /// Создание слота у врача
    /// </summary>
    Task<ScheduleSlotDto> CreateAsync(CreateScheduleSlotDto dto, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Обновление информации у слота
    /// </summary>
    Task<ScheduleSlotDto> UpdateAsync(Guid id, UpdateScheduleSlotDto dto, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Удаление слота
    /// </summary>
    Task DeleteAsync(Guid id, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Получить список слотов доктора
    /// </summary>
    Task<IReadOnlyList<ScheduleSlotDto>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct);
    /// <summary>
    /// Получить все свободные слоты доктора на определенную дату
    /// </summary>
    Task<IReadOnlyList<ScheduleSlotDto>> GetAvailableAsync(Guid doctorId, DateTime date, CancellationToken ct);
}
