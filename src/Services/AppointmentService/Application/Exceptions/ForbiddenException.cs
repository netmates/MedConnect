namespace AppointmentService.Application.Exceptions;

public class ForbiddenException(string message) : Exception(message)
{
}
