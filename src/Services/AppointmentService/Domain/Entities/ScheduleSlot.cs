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
        if (doctorId == Guid.Empty)
            throw new DomainException("DoctorId обязателен.");

        EnsureTimeRange(startTime, endTime);

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

    public void Free()
    {
        if (Status != SlotStatus.Booked)
            throw new DomainException("Освободить можно только забронированный слот.");

        Status = SlotStatus.Available;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Consume()
    {
        if (Status != SlotStatus.Booked)
            throw new DomainException("Отметить использованным можно только забронированный слот.");

        Status = SlotStatus.Consumed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(DateTime startTime, DateTime endTime)
    {
        if (Status != SlotStatus.Available)
            throw new DomainException("Редактировать можно только свободный слот.");

        EnsureTimeRange(startTime, endTime);

        StartTime = startTime;
        EndTime = endTime;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void EnsureTimeRange(DateTime startTime, DateTime endTime)
    {
        if (endTime <= startTime)
            throw new DomainException("Конец слота должен быть позже начала.");

        var durationMinutes = (endTime - startTime).TotalMinutes;
        if (durationMinutes < MinDurationMinutes || durationMinutes > MaxDurationMinutes)
            throw new DomainException(
                $"Длительность слота должна быть от {MinDurationMinutes} до {MaxDurationMinutes} минут.");
    }
}
