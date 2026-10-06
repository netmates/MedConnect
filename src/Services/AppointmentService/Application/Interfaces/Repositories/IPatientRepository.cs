using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Interfaces.Repositories;

public interface IPatientRepository : IRepository<Patient>
{
    /// <summary>
    /// Найти пациента по KeycloakId.
    /// </summary>
    public Task<Patient?> GetByKeycloakIdAsync(string keycloakId, CancellationToken ct = default);
    /// <summary>
    /// Проверяет существует ли уже пользователь при регистрации через OAuth.
    /// </summary>
    // Task<bool> ExistsByKeycloakIdAsync(string keycloakId, CancellationToken ct = default);
    /// <summary>
    /// Получить всех пациентов, включая деактивированных.
    /// </summary>
    public Task<IReadOnlyList<Patient>> GetAllIncludingInactiveAsync(CancellationToken ct = default);
}
