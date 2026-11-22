using MedConnect.Shared.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace ApiGateway.Auth;

public static class GatewayAuthenticationExtensions
{
    private static readonly PathString ChatHubPrefix = new("/api/chats/hub");

    // JWT Keycloak: подпись, издатель, аудитория и срок
    public static IServiceCollection AddGatewayJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = KeycloakConfiguration.GetRequired(configuration, nameof(KeycloakOptions.Authority));
                options.Audience = KeycloakConfiguration.GetRequired(configuration, nameof(KeycloakOptions.Audience));
                options.RequireHttpsMetadata = configuration.GetValue($"{KeycloakOptions.SectionName}:{nameof(KeycloakOptions.RequireHttpsMetadata)}", true);
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = JwtRegisteredClaimNames.PreferredUsername
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ReadSignalRAccessToken
                };
            });

        return services;
    }

    // WebSocket не умеет заголовок Authorization. Токен хаба приходит в query access_token
    private static Task ReadSignalRAccessToken(MessageReceivedContext context)
    {
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;

        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments(ChatHubPrefix))
            context.Token = accessToken;

        return Task.CompletedTask;
    }
}
