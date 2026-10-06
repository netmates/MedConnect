using MedConnect.Shared.Health;
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

    public static void MapCommunicationHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks($"/health/{LiveTag}", new HealthCheckOptions
        {
            Predicate = c => c.Tags.Contains(LiveTag),
            ResponseWriter = HealthCheckResponse.WriteJson
        }).AllowAnonymous();

        app.MapHealthChecks($"/health/{ReadyTag}", new HealthCheckOptions
        {
            Predicate = c => c.Tags.Contains(ReadyTag),
            ResponseWriter = HealthCheckResponse.WriteJson
        }).AllowAnonymous();
    }
}
