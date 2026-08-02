using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;

namespace AppointmentService.Application.Interfaces.Repositories;

public interface IAppointmentRepository : IRepository<Appointment>
{
    /// <summary>
    /// Получить записи пациента. Опционально: status, период по Slot.StartTime (from / to).
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetByPatientIdAsync(
        Guid patientId,
        AppointmentStatus? status = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);
    /// <summary>
    /// Получить записи врача. Опционально: status, период по Slot.StartTime (from / to).
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetByDoctorIdAsync(
        Guid doctorId,
        AppointmentStatus? status = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);
    /// <summary>
    /// Получить запись по id с Doctor, Patient, Slot.
    /// </summary>
    Task<Appointment?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    /// <summary>
    /// Найти запись, привязанную к конкретному слоту; нужно для проверки, занят ли слот.
    /// </summary>
    Task<Appointment?> GetBySlotIdAsync(Guid slotId, CancellationToken ct = default);
    /// <summary>
    /// Получить запись на приём с пессимистической блокировкой.
    /// Защита от race condition: при одновременной отмене, изменении или обработке одной и той же записи
    /// второй запрос будет ждать, пока первый не завершит транзакцию; вызывается только внутри открытой транзакции.
    /// </summary>
    Task<Appointment?> GetByIdWithLockAsync(Guid id, CancellationToken ct = default);
    /// <summary>
    /// Будущие активные записи врача (Created / Confirmed), с Slot.
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetActiveFutureByDoctorIdAsync(
        Guid doctorId,
        DateTime after,
        CancellationToken ct = default);
    /// <summary>
    /// Будущие активные записи пациента (Created / Confirmed), с Slot.
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetActiveFutureByPatientIdAsync(
        Guid patientId,
        DateTime after,
        CancellationToken ct = default);
}
