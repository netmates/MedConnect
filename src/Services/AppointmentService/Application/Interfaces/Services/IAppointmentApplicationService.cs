using AppointmentService.Application.DTOs.Appointment;

namespace AppointmentService.Application.Interfaces.Services;

public interface IAppointmentApplicationService
{
    /// <summary>
    /// Возвращает пациенту список его записей
    /// </summary>
    Task<IReadOnlyList<AppointmentDto>> GetByPatientAsync(string keycloakId, CancellationToken ct);
    /// <summary>
    /// Создает запись
    /// </summary>
    Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Отменяет запись
    /// </summary>
    Task CancelAsync(Guid appointmentId, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Отметить запись как завершённую
    /// </summary>
    Task CompleteAsync(Guid appointmentId, string keycloakId, CancellationToken ct);
    /// <summary>
    /// Подтверждение записи
    /// </summary>
    Task ConfirmAsync(Guid appointmentId, string keycloakId, CancellationToken ct);
}
