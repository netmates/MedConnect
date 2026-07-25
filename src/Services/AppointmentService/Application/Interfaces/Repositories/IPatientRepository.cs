using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Interfaces.Repositories;

public interface IPatientRepository : IRepository<Patient>
{
    /// <summary>
    /// Найти пациента по keycloakId из JWT
    /// </summary>
    Task<Patient?> GetByKeycloakIdAsync(string keycloakId, CancellationToken ct = default);
    // Проверяет существует ли уже пользователь при регистрации через OAuth
    //Task<bool> ExistsByKeycloakIdAsync(string keycloakId, CancellationToken ct = default);
    /// <summary>
    /// Получить всех пациентов, включая деактивированных
    /// </summary>
    Task<IReadOnlyList<Patient>> GetAllWithInactiveAsync(CancellationToken ct = default);
}