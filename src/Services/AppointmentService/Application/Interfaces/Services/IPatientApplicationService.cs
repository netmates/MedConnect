using AppointmentService.Application.DTOs.Patient;

namespace AppointmentService.Application.Interfaces.Services;

public interface IPatientApplicationService
{
    /// <summary>
    /// Регистрация через форму (email + пароль)
    /// </summary>
    Task<PatientDto> RegisterOrGetAsync(string keycloakId, RegisterPatientDto dto, CancellationToken ct);
    /// <summary>
    /// Найти пациента по keycloakId
    /// </summary>
    Task<PatientDto> GetByKeycloakIdAsync(string keycloakId, CancellationToken ct);
    /// <summary>
    /// Обновить данные пациента
    /// </summary>
    Task<PatientDto> UpdateAsync(string keycloakId, UpdatePatientDto dto, CancellationToken ct);
}
