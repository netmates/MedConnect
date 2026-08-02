using AppointmentService.Application.DTOs.Patient;

namespace AppointmentService.Application.Interfaces.Services;

public interface IAdminPatientApplicationService
{
    /// <summary>
    /// Получить всех пациентов, включая деактивированных.
    /// </summary>
    Task<IReadOnlyList<PatientDto>> GetAllIncludingInactiveAsync(CancellationToken ct);
    /// <summary>
    /// Получить пациента по id.
    /// </summary>
    Task<PatientDto> GetByIdAsync(Guid id, CancellationToken ct);
    /// <summary>
    /// Обновить данные пациента.
    /// </summary>
    Task<PatientDto> UpdateAsync(Guid id, UpdatePatientDto dto, CancellationToken ct);
    /// <summary>
    /// Деактивировать пациента.
    /// </summary>
    Task DeactivateAsync(Guid id, CancellationToken ct);
    /// <summary>
    /// Активировать пациента.
    /// </summary>
    Task ActivateAsync(Guid id, CancellationToken ct);
}
