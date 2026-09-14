using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using AppointmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Repositories;

public class AppointmentRepository(AppointmentDbContext context) : Repository<Appointment>(context), IAppointmentRepository
{
    public async Task<IReadOnlyList<Appointment>> GetByPatientIdAsync(
        Guid patientId,
        AppointmentStatus? status = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var query = _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Include(a => a.Slot)
            .Where(a => a.PatientId == patientId);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (from.HasValue)
            query = query.Where(a => a.Slot.StartTime >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.Slot.StartTime < to.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Appointment>> GetByDoctorIdAsync(
        Guid doctorId,
        AppointmentStatus? status = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var query = _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Include(a => a.Slot)
            .Where(a => a.DoctorId == doctorId);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (from.HasValue)
            query = query.Where(a => a.Slot.StartTime >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.Slot.StartTime < to.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Appointment?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Include(a => a.Slot)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<Appointment?> GetBySlotIdAsync(Guid slotId, CancellationToken ct = default)
        => await _context.Appointments
            .FirstOrDefaultAsync(a => a.SlotId == slotId && a.Status != AppointmentStatus.Cancelled, ct);

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

    public async Task<IReadOnlyList<Appointment>> GetActiveByDoctorIdAsync(Guid doctorId, CancellationToken ct = default)
        => await _context.Appointments
            .Where(a => a.DoctorId == doctorId
                    && (a.Status == AppointmentStatus.Created || a.Status == AppointmentStatus.Confirmed))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Appointment>> GetActiveByPatientIdAsync(Guid patientId, CancellationToken ct = default)
        => await _context.Appointments
            .Where(a => a.PatientId == patientId
                    && (a.Status == AppointmentStatus.Created || a.Status == AppointmentStatus.Confirmed))
            .ToListAsync(ct);

    public async Task<bool> ExistsBySlotIdAsync(Guid slotId, CancellationToken ct = default)
        => await _context.Appointments.AnyAsync(a => a.SlotId == slotId, ct);

    public override Task DeleteAsync(Appointment entity, CancellationToken ct = default)
    {
        throw new NotSupportedException(
            "Hard deletion of Appointment is not allowed. Use Cancel() or Complete() instead.");
    }
}
