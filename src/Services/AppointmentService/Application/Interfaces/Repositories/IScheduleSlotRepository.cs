using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Interfaces.Repositories;

public interface IScheduleSlotRepository : IRepository<ScheduleSlot>
{
    // Получить все слоты врача
    Task<IReadOnlyList<ScheduleSlot>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct = default);
    // Получить доступные слоты врача на конкретный день
    Task<IReadOnlyList<ScheduleSlot>> GetAvailableByDoctorIdAsync(Guid doctorId, DateTime date, CancellationToken ct = default);
    // Проверяет, есть ли у врача не отменённый слот, пересекающийся по времени с запрошенным
    Task<bool> HasOverlappingSlotAsync(Guid doctorId, DateTime startTime, DateTime endTime, Guid? excludeSlotId = null, CancellationToken ct = default);
    /*
    Получить слот с пессимистической блокировкой
    Защита от race condition: если два пациента одновременно пытаются забронировать один слот,
    второй запрос будет ждать, пока первый не завершит транзакцию.Вызывается только внутри открытой транзакции
    */
    Task<ScheduleSlot?> GetByIdWithLockAsync(Guid id, CancellationToken ct = default);
}
