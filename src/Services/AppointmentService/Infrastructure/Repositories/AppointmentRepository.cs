using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Domain.Entities;
using AppointmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Repositories;

public class AppointmentRepository(AppointmentDbContext context) : Repository<Appointment>(context), IAppointmentRepository
{
    public async Task<IReadOnlyList<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default)
        => await _context.Appointments
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Appointment>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct = default)
        => await _context.Appointments
            .Where(a => a.DoctorId == doctorId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<Appointment?> GetBySlotIdAsync(Guid slotId, CancellationToken ct = default)
        => await _context.Appointments
            .FirstOrDefaultAsync(a => a.SlotId == slotId, ct);

    public async Task<Appointment?> GetByIdWithLockAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Appointments
        .FromSqlInterpolated($"""
            SELECT *
            FROM "Appointments"
            WHERE "Id" = {id}
            FOR UPDATE
            """)
        .FirstOrDefaultAsync(ct);
    }

    public override Task DeleteAsync(Appointment entity, CancellationToken ct = default)
    {
        throw new NotSupportedException(
            "Hard deletion of Appointment is not allowed. Use Cancel() or Complete() instead.");
    }
}
