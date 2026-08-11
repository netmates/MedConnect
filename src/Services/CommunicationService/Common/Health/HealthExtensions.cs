using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CommunicationService.Common.Health;

public static class HealthExtensions
{
    public static IServiceCollection AddCommunicationHealthChecks(
        this IServiceCollection services)        
    {
        services.AddHealthChecks()
            .AddCheck(
                name: "self",
                () => HealthCheckResult.Healthy("OK"),
                tags: ["live"])
            .AddMongoDb(
                name: "mongodb",
                tags: ["ready"]);

        return services;
    }

    public static void MapCommunicationHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = c => c.Tags.Contains("live")
        }).AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = c => c.Tags.Contains("ready")
        }).AllowAnonymous();
    }
}
