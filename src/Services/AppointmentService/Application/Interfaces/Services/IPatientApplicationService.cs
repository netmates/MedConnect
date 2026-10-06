using AppointmentService.Application.DTOs.Patient;

namespace AppointmentService.Application.Interfaces.Services;

public interface IPatientApplicationService
{
    /// <summary>
    /// Зарегистрировать пациента или вернуть существующий профиль.
    /// </summary>
    public Task<PatientDto> RegisterOrGetAsync(string keycloakId, RegisterPatientDto dto, CancellationToken ct);
    /// <summary>
    /// Найти пациента по KeycloakId.
    /// </summary>
    public Task<PatientDto> GetByKeycloakIdAsync(string keycloakId, CancellationToken ct);
    /// <summary>
    /// Обновить данные пациента.
    /// </summary>
    public Task<PatientDto> UpdateAsync(string keycloakId, UpdatePatientDto dto, CancellationToken ct);
}
