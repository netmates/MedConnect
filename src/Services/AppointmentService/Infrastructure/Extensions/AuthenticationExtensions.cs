using AppointmentService.Infrastructure.Keycloak;
using MedConnect.Shared.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AppointmentService.Infrastructure.Extensions;

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
                    OnTokenValidated = KeycloakRealmRoles.Map
                };
            });

        return services;
    }
}
