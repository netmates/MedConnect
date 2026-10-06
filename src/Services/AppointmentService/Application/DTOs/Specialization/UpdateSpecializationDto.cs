namespace AppointmentService.Application.DTOs.Specialization;

public sealed record UpdateSpecializationDto
{
    public string Name { get; init; } = string.Empty;
}
