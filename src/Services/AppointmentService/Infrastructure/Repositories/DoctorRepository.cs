using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Domain.Entities;
using AppointmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Repositories;

public class DoctorRepository(AppointmentDbContext context) : Repository<Doctor>(context), IDoctorRepository
{
    public async Task<IReadOnlyList<Doctor>> GetBySpecializationAsync(Guid specializationId, CancellationToken ct = default)
        => await _context.Doctors
            .Include(d => d.DoctorSpecializations)
                .ThenInclude(ds => ds.Specialization)
            .Where(d => d.DoctorSpecializations.Any(ds => ds.SpecializationId == specializationId) && d.IsActive)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Doctor>> GetActiveAsync(CancellationToken ct = default)
        => await _context.Doctors
            .Include(d => d.DoctorSpecializations)
                .ThenInclude(ds => ds.Specialization)
            .Where(d => d.IsActive)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Doctor>> GetAllIncludingInactiveAsync(CancellationToken ct = default)
        => await _context.Doctors
            .Include(d => d.DoctorSpecializations)
                .ThenInclude(ds => ds.Specialization)
            .ToListAsync(ct);

    public async Task<Doctor?> GetWithSpecializationsAsync(Guid doctorId, CancellationToken ct = default)
        => await _context.Doctors
            .Include(d => d.DoctorSpecializations)
                .ThenInclude(ds => ds.Specialization)
            .FirstOrDefaultAsync(d => d.Id == doctorId, ct);

    public async Task<Doctor?> GetByKeycloakIdAsync(string keycloakId, CancellationToken ct = default)
        => await _context.Doctors
            .FirstOrDefaultAsync(d => d.KeycloakId == keycloakId, ct);
    
    public async Task AddDoctorSpecializationAsync(DoctorSpecialization doctorSpecialization, CancellationToken ct = default)
        => await _context.DoctorSpecializations.AddAsync(doctorSpecialization, ct);

    public async Task RemoveDoctorSpecializationAsync(Guid doctorId, Guid specializationId, CancellationToken ct = default)
    {
        var doctorSpecialization = await _context.DoctorSpecializations
            .FirstOrDefaultAsync(ds => ds.DoctorId == doctorId && ds.SpecializationId == specializationId, ct);

        if (doctorSpecialization is null) return;

        _context.DoctorSpecializations.Remove(doctorSpecialization);
    }    

    public override Task DeleteAsync(Doctor entity, CancellationToken ct = default)
    {
        throw new NotSupportedException(
            "Hard deletion of Doctor is not allowed. Use Deactivate().");
    }
}
