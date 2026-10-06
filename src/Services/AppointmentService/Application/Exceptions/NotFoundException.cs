namespace AppointmentService.Application.Exceptions;

public sealed class NotFoundException(string message) : Exception(message)
{
}
