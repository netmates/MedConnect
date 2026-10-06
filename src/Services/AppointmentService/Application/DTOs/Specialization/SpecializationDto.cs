namespace AppointmentService.Application.DTOs.Specialization;

public sealed record SpecializationDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
