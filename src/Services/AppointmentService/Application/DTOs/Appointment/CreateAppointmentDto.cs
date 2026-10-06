namespace AppointmentService.Application.DTOs.Appointment;

public sealed record CreateAppointmentDto
{
    public Guid SlotId { get; init; }
    public string? Reason { get; init; }
}
