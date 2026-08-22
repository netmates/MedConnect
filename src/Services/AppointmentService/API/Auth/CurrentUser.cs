using AppointmentService.Application.Exceptions;
using System.Security.Claims;

namespace AppointmentService.API.Auth;

public static class CurrentUser
{
    public static string GetKeycloakId(ClaimsPrincipal user) =>
        user.FindFirstValue("sub")
            ?? throw new ForbiddenException("В токене нет claim sub.");
}
