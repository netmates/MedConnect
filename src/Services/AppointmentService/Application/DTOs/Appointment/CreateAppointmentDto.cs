namespace AppointmentService.Application.DTOs.Appointment;

public class CreateAppointmentDto
{
    public Guid SlotId { get; set; }
    public string? Reason { get; set; }
}