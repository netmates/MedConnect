namespace AppointmentService.Application.DTOs.ScheduleSlot;

public interface IScheduleSlotTime
{
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }
}
