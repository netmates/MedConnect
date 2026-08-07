using AppointmentService.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AppointmentService.Infrastructure.Extensions;

public static class HealthChecksExtensions
{
    public static IServiceCollection AddAppointmentHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var postgres = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres не задан.");

        var adminApiUrl = configuration["Keycloak:AdminApiUrl"]
            ?? throw new InvalidOperationException("Keycloak:AdminApiUrl не задан.");

        var realm = configuration["Keycloak:Realm"]
            ?? throw new InvalidOperationException("Keycloak:Realm не задан.");

        var keycloakRealmUrl = new Uri(
            $"{adminApiUrl.TrimEnd('/')}/realms/{realm}");

        services.AddHealthChecks()
            // --- Liveness: только процесс ---
            .AddCheck(
                name: "self",
                () => HealthCheckResult.Healthy("OK"),
                tags: ["live"])

            // --- Readiness: PostgreSQL ---
            .AddDbContextCheck<AppointmentDbContext>(
                name: "postgres",
                tags: ["ready", "db"])

            // --- Readiness: Keycloak ---
            .AddUrlGroup(
                keycloakRealmUrl,
                name: "keycloak",
                tags: ["ready", "keycloak"]);

        return services;
    }

    public static Task WriteHealthJson(HttpContext context, HealthReport report)
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
}
