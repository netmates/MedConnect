namespace AppointmentService.Application.DTOs.Specialization;

public sealed record CreateSpecializationDto
{
    public string Name { get; init; } = string.Empty;
}
