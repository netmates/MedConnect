using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace CommunicationService.Common.Auth;

public static class AuthenticationExtensions
{
    /// <summary>
    /// JWT Bearer через Keycloak: Authority/Audience, без MapInboundClaims (claim sub как в токене),
    /// роли из realm_access → claim role для [Authorize(Roles = "...")].
    /// </summary>
    public static IServiceCollection AddKeycloakJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Keycloak:Authority"];
                options.Audience = configuration["Keycloak:Audience"];
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    RoleClaimType = "role",
                    NameClaimType = "preferred_username"
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = MapKeycloakRealmRoles
                };
            });

        return services;
    }

    /// <summary>
    /// Keycloak кладёт роли в claim realm_access (JSON: { "roles": ["admin", ...] }).
    /// Добавляем каждую роль как отдельный claim "role" для [Authorize(Roles = "...")].
    /// </summary>
    private static Task MapKeycloakRealmRoles(TokenValidatedContext context)
    {
        var identity = context.Principal?.Identity as ClaimsIdentity;
        if (identity is null)
            return Task.CompletedTask;

        var realmAccessClaim = context.Principal!.FindFirst("realm_access")?.Value;
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
