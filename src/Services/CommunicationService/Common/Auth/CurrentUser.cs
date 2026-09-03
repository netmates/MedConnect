using CommunicationService.Common.Exceptions;
using System.Security.Claims;

namespace CommunicationService.Common.Auth;

public static class CurrentUser
{
    public static string GetKeycloakId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(sub))
            throw new ForbiddenException("В токене нет claim sub.");

        return sub;
    }

    public static string GetSenderRole(ClaimsPrincipal user)
    {
        if (user.IsInRole(Roles.Doctor))
            return Roles.Doctor;

        if (user.IsInRole(Roles.Patient))
            return Roles.Patient;

        throw new ForbiddenException($"Нужна роль {Roles.Patient} или {Roles.Doctor}.");
    }
}
