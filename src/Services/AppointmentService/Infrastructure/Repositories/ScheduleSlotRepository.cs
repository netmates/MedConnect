using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using AppointmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Repositories;

public class ScheduleSlotRepository(AppointmentDbContext context) : Repository<ScheduleSlot>(context), IScheduleSlotRepository
{
    public async Task<IReadOnlyList<ScheduleSlot>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct = default)
        => await _context.ScheduleSlots
            .Where(s => s.DoctorId == doctorId)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ScheduleSlot>> GetAvailableByDoctorIdAsync(Guid doctorId, DateTime date, CancellationToken ct = default)
    {
        // Диапазон вместо StartTime.Date == date.Date        
        // Диапазон >= / < транслируется в простое сравнение и задействует индекс по StartTime
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);
        return await _context.ScheduleSlots
            .Where(s => s.DoctorId == doctorId
                     && s.Status == SlotStatus.Available
                     && s.StartTime >= startOfDay
                     && s.StartTime < endOfDay)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);
    }
    
    public async Task<bool> HasOverlappingSlotAsync(
        Guid doctorId,
        DateTime startTime,
        DateTime endTime,
        Guid? excludeSlotId = null,
        CancellationToken ct = default)
        => await _context.ScheduleSlots
            .AnyAsync(s => s.DoctorId == doctorId
                        && s.Status != SlotStatus.Cancelled
                        && s.StartTime < endTime
                        && s.EndTime > startTime
                        // excludeSlotId - чтобы не найти самого себя при редактировании
                        && (excludeSlotId == null || s.Id != excludeSlotId), ct);
    
    public async Task<ScheduleSlot?> GetByIdWithLockAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.ScheduleSlots
        .FromSqlInterpolated($"""
            SELECT *
            FROM "ScheduleSlots"
            WHERE "Id" = {id}
            FOR UPDATE
            """)
        .FirstOrDefaultAsync(ct);
    }
}
