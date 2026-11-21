using CommunicationService.Common.Persistence;
using MedConnect.Shared.Consuming;

namespace CommunicationService.Common.Consuming;

public sealed class MongoRetryIntegrationEventHandler<TPayload>(
    IIntegrationEventHandler<TPayload> inner,
    ILogger<MongoRetryIntegrationEventHandler<TPayload>> logger) : IIntegrationEventHandler<TPayload>
{
    public Task HandleAsync(TPayload payload, CancellationToken ct) =>
        MongoConsumerRetry.ExecuteAsync(
            retryCt => inner.HandleAsync(payload, retryCt),
            logger,
            ct);
}
