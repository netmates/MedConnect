using System.Security.Claims;

namespace CommunicationService.Common.Auth;

public static class CurrentUser
{
    public static string GetKeycloakId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(sub))
            throw new InvalidOperationException("В токене нет claim sub.");

        return sub;
    }

    public static string GetSenderRole(ClaimsPrincipal user)
    {
        if (user.IsInRole("doctor"))
            return "doctor";

        if (user.IsInRole("patient"))
            return "patient";

        throw new InvalidOperationException("Нужна роль patient или doctor.");
    }
}
