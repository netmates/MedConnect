using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Interfaces.Repositories;

public interface IDoctorRepository : IRepository<Doctor>
{
    /// <summary>
    /// Получить список активных врачей, работающих по указанной специализации.
    /// </summary>
    Task<IReadOnlyList<Doctor>> GetBySpecializationAsync(Guid specializationId, CancellationToken ct = default);
    /// <summary>
    /// Получить список активных врачей.
    /// </summary>
    Task<IReadOnlyList<Doctor>> GetActiveAsync(CancellationToken ct = default);
    /// <summary>
    /// Получить врача вместе с его специализациями.
    /// </summary>    
    Task<Doctor?> GetWithSpecializationsAsync(Guid doctorId, CancellationToken ct = default);
    /// <summary>
    /// Найти врача по KeycloakId.
    /// </summary>
    Task<Doctor?> GetByKeycloakIdAsync(string keycloakId, CancellationToken ct = default);
    /// <summary>
    /// Добавить связь «врач - специализация».
    /// </summary>
    Task AddDoctorSpecializationAsync(DoctorSpecialization doctorSpecialization, CancellationToken ct = default);
    /// <summary>
    /// Удалить связь врача со специализацией.
    /// </summary>    
    Task RemoveDoctorSpecializationAsync(Guid doctorId, Guid specializationId, CancellationToken ct = default);
    /// <summary>
    /// Получить всех врачей (включая деактивированных) с загруженными специализациями.
    /// </summary>    
    Task<IReadOnlyList<Doctor>> GetAllIncludingInactiveAsync(CancellationToken ct = default);
}
