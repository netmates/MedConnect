using AppointmentService.Application.DTOs.Appointment;
using AppointmentService.Domain.Enums;

namespace AppointmentService.Application.Interfaces.Services;

public interface IAppointmentApplicationService
{
    /// <summary>
    /// Получить список записей пациента. Опционально: status, период по Slot.StartTime (from / to).
    /// </summary>
    Task<IReadOnlyList<AppointmentDto>> GetByPatientAsync(
        string keycloakId,
        AppointmentStatus? status,
        DateTime? from,
        DateTime? to,
        CancellationToken ct);
    /// <summary>
    /// Получить список записей врача. Опционально: status, период по Slot.StartTime (from / to).
    /// </summary>
    Task<IReadOnlyList<AppointmentDto>> GetByDoctorAsync(
        string keycloakId,
        AppointmentStatus? status,
        DateTime? from,
        DateTime? to,
        CancellationToken ct);
    /// <summary>
    /// Получить запись по id. Доступ только владельцу (пациент или врач этой записи).
    /// </summary>
    Task<AppointmentDto> GetByIdAsync(Guid appointmentId, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Создать запись на приём.
    /// </summary>
    Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Отменить запись (пациент или врач).
    /// </summary>
    Task CancelAsync(Guid appointmentId, string keycloakId, CancellationToken ct);    
    /// <summary>
    /// Завершить приём.
    /// </summary>
    Task CompleteAsync(Guid appointmentId, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Подтвердить запись.
    /// </summary>
    Task ConfirmAsync(Guid appointmentId, string keycloakId, CancellationToken ct);
}
