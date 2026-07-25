using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Interfaces.Repositories;

public interface IAppointmentRepository : IRepository<Appointment>
{
    /// <summary>
    /// Получить все записи конкретного пациента
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default);
    /// <summary>
    /// Получить все записи к конкретному врачу
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct = default);
    /// <summary>
    /// Найти запись, привязанную к конкретному слоту; нужно для проверки, занят ли слот
    /// </summary>
    Task<Appointment?> GetBySlotIdAsync(Guid slotId, CancellationToken ct = default);
    /// <summary>
    /// Получить запись на приём с пессимистической блокировкой.
    /// Защита от race condition: при одновременной отмене, изменении или обработке одной и той же записи
    /// второй запрос будет ждать, пока первый не завершит транзакцию; вызывается только внутри открытой транзакции    
    /// </summary>
    Task<Appointment?> GetByIdWithLockAsync(Guid id, CancellationToken ct = default);
}
