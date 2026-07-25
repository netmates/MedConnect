using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Interfaces.Repositories;

public interface IScheduleSlotRepository : IRepository<ScheduleSlot>
{
    /// <summary>
    /// Получить все слоты врача
    /// </summary>
    Task<IReadOnlyList<ScheduleSlot>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct = default);
    /// <summary>
    /// Получить доступные слоты врача на конкретный день
    /// </summary>
    Task<IReadOnlyList<ScheduleSlot>> GetAvailableByDoctorIdAsync(Guid doctorId, DateTime date, CancellationToken ct = default);
    /// <summary>
    /// Проверяет, есть ли у врача не отменённый слот, пересекающийся по времени с запрошенным
    /// </summary>
    Task<bool> HasOverlappingSlotAsync(Guid doctorId, DateTime startTime, DateTime endTime, Guid? excludeSlotId = null, CancellationToken ct = default);
    /// <summary>
    /// Получить слот с пессимистической блокировкой.
    /// Защита от race condition: если два пациента одновременно пытаются забронировать один слот,
    /// второй запрос будет ждать, пока первый не завершит транзакцию; вызывается только внутри открытой транзакции
    /// </summary>
    Task<ScheduleSlot?> GetByIdWithLockAsync(Guid id, CancellationToken ct = default);
}
