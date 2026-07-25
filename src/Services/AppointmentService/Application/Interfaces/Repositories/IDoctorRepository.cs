using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Interfaces.Repositories;

public interface IDoctorRepository : IRepository<Doctor>
{
    /// <summary>
    /// Получить список активных врачей, работающих по указанной специализации
    /// </summary>
    Task<IReadOnlyList<Doctor>> GetBySpecializationAsync(Guid specializationId, CancellationToken ct = default);
    /// <summary>
    /// Получить врача вместе с его специализациями
    /// </summary>    
    Task<Doctor?> GetWithSpecializationsAsync(Guid doctorId, CancellationToken ct = default);
    /// <summary>
    /// Найти врача по keycloakId из JWT
    /// </summary>
    Task<Doctor?> GetByKeycloakIdAsync(string keycloakId, CancellationToken ct = default);
    /// <summary>
    /// Добавить связь «врач - специализация»
    /// </summary>
    Task AddDoctorSpecializationAsync(DoctorSpecialization doctorSpecialization, CancellationToken ct = default);
    /// <summary>
    /// Удаляет связь врача со специализацией.
    /// Проверяет: врач должен иметь ≥1 специализацию после удаления
    /// </summary>    
    Task RemoveDoctorSpecializationAsync(Guid doctorId, Guid specializationId, CancellationToken ct = default);
    /// <summary>
    /// Получить всех врачей (включая деактивированных) с загруженными специализациями
    /// </summary>    
    Task<IReadOnlyList<Doctor>> GetAllIncludingInactiveAsync(CancellationToken ct = default);
}
