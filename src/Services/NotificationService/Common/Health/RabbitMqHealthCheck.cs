using MedConnect.Shared.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NotificationService.Common.Health;

public sealed class RabbitMqHealthCheck(RabbitMqConnection connection) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            var rabbitConnection = await connection.GetConnectionAsync(ct);
            return rabbitConnection.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ connection is open")
                : HealthCheckResult.Unhealthy("RabbitMQ connection is closed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ connection failed", ex);
        }
    }
}
