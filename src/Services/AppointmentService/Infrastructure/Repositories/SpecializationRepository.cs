using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Domain.Entities;
using AppointmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Repositories;

public class SpecializationRepository(AppointmentDbContext context) : Repository<Specialization>(context), ISpecializationRepository
{
    // Проверяет наличие активных врачей с данной специализацией    
    public async Task<bool> HasAnyDoctorsAsync(Guid specializationId, CancellationToken ct = default)
        => await _context.DoctorSpecializations
            .AnyAsync(ds => ds.SpecializationId == specializationId, ct);
}
