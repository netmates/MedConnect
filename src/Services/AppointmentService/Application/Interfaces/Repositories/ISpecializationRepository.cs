using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Interfaces.Repositories;

public interface ISpecializationRepository : IRepository<Specialization>
{
    /// <summary>
    /// Проверяет наличие врачей с данной специализацией.
    /// Используется перед удалением записи из справочника
    /// </summary>
    Task<bool> HasAnyDoctorsAsync(Guid specializationId, CancellationToken ct = default);
}
