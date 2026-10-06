using AppointmentService.Application.DTOs.Appointment;
using AppointmentService.Domain.Enums;

namespace AppointmentService.Application.Interfaces.Services;

public interface IAppointmentApplicationService
{
    /// <summary>
    /// Получить список записей пациента. Опционально: status, период по Slot.StartTime (from / to).
    /// </summary>
    public Task<IReadOnlyList<AppointmentDto>> GetByPatientAsync(
        string keycloakId,
        AppointmentStatus? status,
        DateTime? from,
        DateTime? to,
        CancellationToken ct);
    /// <summary>
    /// Получить список записей врача. Опционально: status, период по Slot.StartTime (from / to).
    /// </summary>
    public Task<IReadOnlyList<AppointmentDto>> GetByDoctorAsync(
        string keycloakId,
        AppointmentStatus? status,
        DateTime? from,
        DateTime? to,
        CancellationToken ct);
    /// <summary>
    /// Получить запись по id. Доступ только владельцу (пациент или врач этой записи).
    /// </summary>
    public Task<AppointmentDto> GetByIdAsync(Guid appointmentId, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Создать запись на прием.
    /// </summary>
    public Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Отменить запись (пациент или врач).
    /// </summary>
    public Task CancelAsync(Guid appointmentId, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Завершить прием.
    /// </summary>
    public Task CompleteAsync(Guid appointmentId, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Подтвердить запись.
    /// </summary>
    public Task ConfirmAsync(Guid appointmentId, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Проверка доступа к записи для CommunicationService (gRPC):
    /// запись существует, пользователь (keycloakId) — ее пациент или врач,
    /// статус не Cancelled/Completed.
    /// </summary>
    public Task<ValidateAppointmentAccessResult> ValidateAccessAsync(
        Guid appointmentId,
        string keycloakId,
        CancellationToken ct);
}
