using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Interfaces.Repositories;

public interface IPatientRepository : IRepository<Patient>
{
    // Найти пациента по keycloakId из JWT
    //Task<Patient?> GetByKeycloakIdAsync(string keycloakId, CancellationToken ct = default);

    // Получить всех пациентов, включая деактивированных
    Task<IReadOnlyList<Patient>> GetAllWithInactiveAsync(CancellationToken ct = default);
}