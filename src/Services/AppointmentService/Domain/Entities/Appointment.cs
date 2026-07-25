using AppointmentService.Domain.Enums;
using AppointmentService.Domain.Exceptions;

namespace AppointmentService.Domain.Entities;

public class Appointment
{
    public const int MaxReasonLength = 500;

    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid SlotId { get; private set; }
    public string? Reason { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Patient Patient { get; private set; } = null!;
    public Doctor Doctor { get; private set; } = null!;
    public ScheduleSlot Slot { get; private set; } = null!;

    private Appointment() { }

    public static Appointment Create(
        Guid patientId,
        Guid doctorId,
        Guid slotId,
        string? reason)
    {
        return new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = doctorId,
            SlotId = slotId,
            Reason = reason,
            Status = AppointmentStatus.Created,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Confirm()
    {
        if (Status != AppointmentStatus.Created)
            throw new DomainException("Подтвердить можно только созданную запись.");
        Status = AppointmentStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
            throw new DomainException("Нельзя отменить завершённую или уже отменённую запись.");
        Status = AppointmentStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {   
        if (Status != AppointmentStatus.Confirmed)
            throw new DomainException($"Завершить можно только подтверждённую запись. Текущий статус: {Status}.");
        Status = AppointmentStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }
}
