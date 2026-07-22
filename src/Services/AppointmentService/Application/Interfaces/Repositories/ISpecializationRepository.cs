using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Interfaces.Repositories;

public interface ISpecializationRepository : IRepository<Specialization>
{
    // Проверяет наличие врачей с данной специализацией
    // Используется перед удалением записи из справочника
    Task<bool> HasAnyDoctorsAsync(Guid specializationId, CancellationToken ct = default);
}
