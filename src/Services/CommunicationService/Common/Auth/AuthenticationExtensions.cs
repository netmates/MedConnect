using CommunicationService.Common.SignalR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;

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
                options.Authority = KeycloakConfiguration.GetRequired(configuration, nameof(KeycloakOptions.Authority));
                options.Audience = KeycloakConfiguration.GetRequired(configuration, nameof(KeycloakOptions.Audience));
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    RoleClaimType = "role",
                    NameClaimType = "preferred_username"
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ReadSignalRAccessToken,
                    OnTokenValidated = MapKeycloakRealmRoles
                };
            });

        return services;
    }

    /// <summary>
    /// Keycloak кладет роли в claim realm_access (JSON: { "roles": ["admin", ...] }).
    /// Добавляем каждую роль как отдельный claim "role" для [Authorize(Roles = "...")].
    /// </summary>
    private static Task MapKeycloakRealmRoles(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
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

    /// <summary>
    /// SignalR: JWT из query access_token (WebSocket без заголовка Authorization).
    /// Подставляем context.Token только для пути хаба (включая /negotiate).
    /// </summary>
    private static Task ReadSignalRAccessToken(MessageReceivedContext context)
    {
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;

        if (!string.IsNullOrEmpty(accessToken)
            && path.StartsWithSegments(ChatHubPaths.Hub))
        {
            context.Token = accessToken;
        }

        return Task.CompletedTask;
    }
}
