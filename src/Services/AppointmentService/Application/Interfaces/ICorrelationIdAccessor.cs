namespace AppointmentService.Application.Interfaces;

public interface ICorrelationIdAccessor
{
    public string? Get();
}
