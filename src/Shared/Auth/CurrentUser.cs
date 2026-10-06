using System.Security.Claims;

namespace MedConnect.Shared.Auth;

public static class CurrentUser
{
    public static string GetKeycloakId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(sub))
            throw new ForbiddenException("В токене нет claim sub.");

        return sub;
    }
}
