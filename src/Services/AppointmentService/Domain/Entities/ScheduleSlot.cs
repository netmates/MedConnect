using AppointmentService.Domain.Enums;
using AppointmentService.Domain.Exceptions;

namespace AppointmentService.Domain.Entities;

public class ScheduleSlot
{
    public const int MinDurationMinutes = 15;
    public const int MaxDurationMinutes = 120;

    public Guid Id { get; private set; }
    public Guid DoctorId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public SlotStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Doctor Doctor { get; private set; } = null!;

    private ScheduleSlot() { }

    public static ScheduleSlot Create(Guid doctorId, DateTime startTime, DateTime endTime)
    {
        return new ScheduleSlot
        {
            Id = Guid.NewGuid(),
            DoctorId = doctorId,
            StartTime = startTime,
            EndTime = endTime,
            Status = SlotStatus.Available,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Book()
    {
        if (Status != SlotStatus.Available)
            throw new DomainException("Слот недоступен для бронирования.");
        Status = SlotStatus.Booked;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {   
        if (Status == SlotStatus.Cancelled)
            throw new DomainException("Слот уже отменён.");
        Status = SlotStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Free()
    {
        Status = SlotStatus.Available;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Update(DateTime startTime, DateTime endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
        UpdatedAt = DateTime.UtcNow;
    }
}
