using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MedConnect.Shared.Health;

public static class HealthCheckResponse
{
    public static Task WriteJson(HttpContext context, HealthReport report)
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
