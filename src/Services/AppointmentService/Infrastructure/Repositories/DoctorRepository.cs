using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Domain.Entities;
using AppointmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Repositories;

public class DoctorRepository(AppointmentDbContext context) : Repository<Doctor>(context), IDoctorRepository
{
    public async Task<IReadOnlyList<Doctor>> GetBySpecializationAsync(Guid specializationId, CancellationToken ct = default)
        => await _context.Doctors
            .Where(d => d.DoctorSpecializations.Any(ds => ds.SpecializationId == specializationId) && d.IsActive)
            .ToListAsync(ct);

    public async Task<Doctor?> GetWithSpecializationsAsync(Guid doctorId, CancellationToken ct = default)
        => await _context.Doctors
            .Include(d => d.DoctorSpecializations)
                .ThenInclude(ds => ds.Specialization)
            .FirstOrDefaultAsync(d => d.Id == doctorId, ct);

    //public async Task<Doctor?> GetByKeycloakIdAsync(string keycloakId, CancellationToken ct = default)
    //    => await _context.Doctors
    //        .FirstOrDefaultAsync(d => d.KeycloakId == keycloakId, ct);
    
    public async Task AddDoctorSpecializationAsync(DoctorSpecialization doctorSpecialization, CancellationToken ct = default)
        => await _context.DoctorSpecializations.AddAsync(doctorSpecialization, ct);

    public async Task<IReadOnlyList<Doctor>> GetAllIncludingInactiveAsync(CancellationToken ct = default)
        => await _context.Doctors
            .Include(d => d.DoctorSpecializations)
                .ThenInclude(ds => ds.Specialization)
            .OrderBy(d => d.LastName)
            .ToListAsync(ct);
}
