using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Domain.Entities;
using AppointmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Repositories;

public class PatientRepository(AppointmentDbContext context) : Repository<Patient>(context), IPatientRepository
{
    public async Task<Patient?> GetByKeycloakIdAsync(string keycloakId, CancellationToken ct = default)
        => await _context.Patients
            .FirstOrDefaultAsync(p => p.KeycloakId == keycloakId, ct);

    //public async Task<bool> ExistsByKeycloakIdAsync(string keycloakId, CancellationToken ct = default)
    //    => await _context.Patients
    //        .AnyAsync(p => p.KeycloakId == keycloakId, ct);

    public async Task<IReadOnlyList<Patient>> GetAllWithInactiveAsync(CancellationToken ct = default)
        => await _context.Patients
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public override Task DeleteAsync(Patient entity, CancellationToken ct = default)
    {
        throw new NotSupportedException(
            "Hard deletion of Patient is not allowed. Use Deactivate() + UpdateAsync().");
    }
}
