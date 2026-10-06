using AppointmentService.Application.DTOs.Patient;

namespace AppointmentService.Application.Interfaces.Services;

public interface IAdminPatientApplicationService
{
    /// <summary>
    /// Получить всех пациентов, включая деактивированных.
    /// </summary>
    public Task<IReadOnlyList<PatientDto>> GetAllIncludingInactiveAsync(CancellationToken ct);
    /// <summary>
    /// Получить пациента по id.
    /// </summary>
    public Task<PatientDto> GetByIdAsync(Guid id, CancellationToken ct);
    /// <summary>
    /// Обновить данные пациента.
    /// </summary>
    public Task<PatientDto> UpdateAsync(Guid id, UpdatePatientDto dto, CancellationToken ct);
    /// <summary>
    /// Деактивировать пациента.
    /// </summary>
    public Task DeactivateAsync(Guid id, CancellationToken ct);
    /// <summary>
    /// Активировать пациента.
    /// </summary>
    public Task ActivateAsync(Guid id, CancellationToken ct);
}
