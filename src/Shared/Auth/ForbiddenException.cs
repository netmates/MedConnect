namespace MedConnect.Shared.Auth;

public sealed class ForbiddenException(string message) : Exception(message);
