using AppointmentService.Application.DTOs.Patient;

namespace AppointmentService.Application.Interfaces.Services;

public interface IAdminPatientApplicationService
{
    /// <summary>
    /// Получить всех пациентов
    /// </summary>
    Task<IReadOnlyList<PatientDto>> GetAllIncludingInactiveAsync(CancellationToken ct);
    /// <summary>
    /// Получить данные пациента
    /// </summary>
    Task<PatientDto> GetByIdAsync(Guid id, CancellationToken ct);
    /// <summary>
    /// Обновить данные пациента
    /// </summary>
    Task<PatientDto> UpdateAsync(Guid id, UpdatePatientDto dto, CancellationToken ct);
    /// <summary>
    /// Деактивация пациента
    /// </summary>
    Task DeactivateAsync(Guid id, CancellationToken ct);
    /// <summary>
    /// Активация пациента
    /// </summary>
    Task ActivateAsync(Guid id, CancellationToken ct);
}
