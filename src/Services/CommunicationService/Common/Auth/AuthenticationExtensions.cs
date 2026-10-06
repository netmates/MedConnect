using CommunicationService.Common.SignalR;
using MedConnect.Shared.Auth;
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
                options.Authority = KeycloakConfiguration.GetRequired(configuration, nameof(KeycloakOptions.Authority));
                options.Audience = KeycloakConfiguration.GetRequired(configuration, nameof(KeycloakOptions.Audience));
                options.RequireHttpsMetadata = configuration.GetValue($"{KeycloakOptions.SectionName}:RequireHttpsMetadata", true);
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    RoleClaimType = "role",
                    NameClaimType = "preferred_username"
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ReadSignalRAccessToken,
                    OnTokenValidated = KeycloakRealmRoles.Map
                };
            });

        return services;
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
