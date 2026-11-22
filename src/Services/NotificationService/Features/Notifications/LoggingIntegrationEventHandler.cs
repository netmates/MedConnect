using MedConnect.Shared.Consuming;

namespace NotificationService.Features.Notifications;

public sealed class LoggingIntegrationEventHandler<TPayload>(
    IIntegrationEventHandler<TPayload> inner,
    ILogger<LoggingIntegrationEventHandler<TPayload>> logger) : IIntegrationEventHandler<TPayload>
{
    public async Task HandleAsync(TPayload payload, CancellationToken ct)
    {
        logger.LogInformation("Handling integration event {EventType}", typeof(TPayload).Name);
        await inner.HandleAsync(payload, ct);
    }
}
