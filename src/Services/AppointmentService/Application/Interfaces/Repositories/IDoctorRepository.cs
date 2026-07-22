using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Interfaces.Repositories;

public interface IDoctorRepository : IRepository<Doctor>
{
    // Получить список активных врачей, работающих по указанной специализации
    Task<IReadOnlyList<Doctor>> GetBySpecializationAsync(Guid specializationId, CancellationToken ct = default);
    // Получить врача вместе с его специализациями
    Task<Doctor?> GetWithSpecializationsAsync(Guid doctorId, CancellationToken ct = default);
    // Найти врача по keycloakId из JWT
    //Task<Doctor?> GetByKeycloakIdAsync(string keycloakId, CancellationToken ct = default);    

    // Добавить связь «врач - специализация»
    Task AddDoctorSpecializationAsync(DoctorSpecialization doctorSpecialization, CancellationToken ct = default);

    // Получить всех врачей (включая деактивированных) с загруженными специализациями
    Task<IReadOnlyList<Doctor>> GetAllIncludingInactiveAsync(CancellationToken ct = default);
}
