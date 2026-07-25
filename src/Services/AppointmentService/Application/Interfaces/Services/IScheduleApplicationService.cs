using AppointmentService.Application.DTOs.Slot;

namespace AppointmentService.Application.Interfaces.Services;

public interface IScheduleApplicationService
{
    /// <summary>
    /// Создание слота у врача
    /// </summary>
    Task<SlotDto> CreateAsync(CreateSlotDto dto, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Обновление информации у слота
    /// </summary>
    Task<SlotDto> UpdateAsync(Guid id, UpdateSlotDto dto, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Удаление слота
    /// </summary>
    Task DeleteAsync(Guid id, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Получить список слотов доктора
    /// </summary>
    Task<IReadOnlyList<SlotDto>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct);
    /// <summary>
    /// Получить все свободные слоты доктора на определенную дату
    /// </summary>
    Task<IReadOnlyList<SlotDto>> GetAvailableAsync(Guid doctorId, DateTime date, CancellationToken ct);
}
