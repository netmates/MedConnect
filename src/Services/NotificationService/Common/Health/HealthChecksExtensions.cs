using MedConnect.Shared.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NotificationService.Common.Health;

public static class HealthChecksExtensions
{
    private const string LiveTag = "live";
    private const string ReadyTag = "ready";

    public static IServiceCollection AddNotificationHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck(
                name: "self",
                () => HealthCheckResult.Healthy("OK"),
                tags: [LiveTag])
            .AddCheck<RabbitMqHealthCheck>(
                name: "rabbitmq",
                tags: [ReadyTag]);

        return services;
    }

    public static void MapNotificationHealthChecks(this WebApplication app)
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
