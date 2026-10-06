using AppointmentService.Infrastructure.Keycloak;
using AppointmentService.Infrastructure.Persistence;
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

    private static Task WriteHealthJson(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description
                    ?? (entry.Value.Status == HealthStatus.Healthy ? "OK" : "Failed"),
                durationMs = entry.Value.Duration.TotalMilliseconds
            })
        };
        return context.Response.WriteAsJsonAsync(payload);
    }

    public static void MapAppointmentHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks($"/health/{LiveTag}", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(LiveTag),
            ResponseWriter = WriteHealthJson
        }).AllowAnonymous();

        app.MapHealthChecks($"/health/{ReadyTag}", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag),
            ResponseWriter = WriteHealthJson
        }).AllowAnonymous();
    }
}
