using AppointmentService.Infrastructure.Keycloak;
using AppointmentService.Infrastructure.Persistence;
using MedConnect.Shared.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AppointmentService.Infrastructure.Extensions;

public static class HealthChecksExtensions
{
    private const string LiveTag = "live";
    private const string ReadyTag = "ready";

    public static IServiceCollection AddAppointmentHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var keycloakRealmUrl = KeycloakConfiguration.GetRealmUri(configuration);

        services.AddHealthChecks()
            // --- Liveness: только процесс ---
            .AddCheck(
                name: "self",
                () => HealthCheckResult.Healthy("OK"),
                tags: [LiveTag])

            // --- Readiness: PostgreSQL ---
            .AddDbContextCheck<AppointmentDbContext>(
                name: "postgres",
                tags: [ReadyTag, "db"])

            // --- Readiness: Keycloak ---
            .AddUrlGroup(
                keycloakRealmUrl,
                name: "keycloak",
                tags: [ReadyTag, "keycloak"]);

        return services;
    }

    public static void MapAppointmentHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks($"/health/{LiveTag}", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(LiveTag),
            ResponseWriter = HealthCheckResponse.WriteJson
        }).AllowAnonymous();

        app.MapHealthChecks($"/health/{ReadyTag}", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag),
            ResponseWriter = HealthCheckResponse.WriteJson
        }).AllowAnonymous();
    }
}
