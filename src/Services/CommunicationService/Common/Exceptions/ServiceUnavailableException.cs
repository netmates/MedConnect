namespace CommunicationService.Common.Exceptions;

public sealed class ServiceUnavailableException(string message) : Exception(message);
