using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace MedConnect.Shared.Auth;

public static class KeycloakRealmRoles
{
    /// <summary>
    /// Keycloak кладет роли в claim realm_access (JSON: { "roles": ["admin", ...] }).
    /// Добавляем каждую роль как отдельный claim "role" для [Authorize(Roles = "...")].
    /// </summary>
    public static Task Map(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
            return Task.CompletedTask;

        var realmAccessClaim = context.Principal.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccessClaim))
            return Task.CompletedTask;

        using var document = JsonDocument.Parse(realmAccessClaim);
        if (!document.RootElement.TryGetProperty("roles", out var rolesElement)
            || rolesElement.ValueKind != JsonValueKind.Array)
            return Task.CompletedTask;

        foreach (var roleElement in rolesElement.EnumerateArray())
        {
            var role = roleElement.GetString();
            if (string.IsNullOrWhiteSpace(role))
                continue;

            if (!identity.HasClaim("role", role))
                identity.AddClaim(new Claim("role", role));
        }

        return Task.CompletedTask;
    }
}
