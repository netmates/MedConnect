using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CommunicationService.Common.Health;

public static class HealthChecksExtensions
{
    private const string LiveTag = "live";
    private const string ReadyTag = "ready";

    public static IServiceCollection AddCommunicationHealthChecks(
        this IServiceCollection services)        
    {
        services.AddHealthChecks()
            // --- Liveness: только процесс ---
            .AddCheck(
                name: "self",
                () => HealthCheckResult.Healthy("OK"),
                tags: [LiveTag])
            // --- Readiness: MongoDB ---
            .AddMongoDb(
                name: "mongodb",
                tags: [ReadyTag]);

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

    public static void MapCommunicationHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks($"/health/{LiveTag}", new HealthCheckOptions
        {
            Predicate = c => c.Tags.Contains(LiveTag),
            ResponseWriter = WriteHealthJson
        }).AllowAnonymous();

        app.MapHealthChecks($"/health/{ReadyTag}", new HealthCheckOptions
        {
            Predicate = c => c.Tags.Contains(ReadyTag),
            ResponseWriter = WriteHealthJson
        }).AllowAnonymous();
    }
}
