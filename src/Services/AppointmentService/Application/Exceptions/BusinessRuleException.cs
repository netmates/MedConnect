namespace AppointmentService.Application.Exceptions;

public sealed class BusinessRuleException(string message) : Exception(message)
{
}
