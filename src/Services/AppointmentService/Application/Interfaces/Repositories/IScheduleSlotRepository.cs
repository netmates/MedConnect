using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Interfaces.Repositories;

public interface IScheduleSlotRepository : IRepository<ScheduleSlot>
{
    /// <summary>
    /// Получить все слоты врача.
    /// </summary>
    public Task<IReadOnlyList<ScheduleSlot>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct = default);
    /// <summary>
    /// Получить доступные слоты врача на конкретный день.
    /// </summary>
    public Task<IReadOnlyList<ScheduleSlot>> GetAvailableByDoctorIdAsync(Guid doctorId, DateTime date, CancellationToken ct = default);
    /// <summary>
    /// Проверить, есть ли у врача слот, пересекающийся по времени с запрошенным.
    /// excludeSlotId позволяет исключить текущий слот при обновлении.
    /// </summary>
    public Task<bool> HasOverlappingSlotAsync(Guid doctorId, DateTime startTime, DateTime endTime, Guid? excludeSlotId = null, CancellationToken ct = default);
    /// <summary>
    /// Получить слот с пессимистической блокировкой.
    /// Защита от race condition: если два пациента одновременно пытаются забронировать один слот,
    /// второй запрос будет ждать, пока первый не завершит транзакцию; вызывается только внутри открытой транзакции.
    /// </summary>
    public Task<ScheduleSlot?> GetByIdWithLockAsync(Guid id, CancellationToken ct = default);
}
