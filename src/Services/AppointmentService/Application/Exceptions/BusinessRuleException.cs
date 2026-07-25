namespace AppointmentService.Application.Exceptions;

public class BusinessRuleException(string message) : Exception(message)
{
}
