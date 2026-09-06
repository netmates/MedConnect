using System.Text;
using System.Text.Json;
using AppointmentService.Application.Interfaces.Services;
using MedConnect.Shared.Events;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AppointmentService.Infrastructure.Messaging;

public sealed class RabbitMqPublisher(
    RabbitMqConnection connection,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqPublisher> logger) : IIntegrationEventPublisher
{
    private const string EventSource = "AppointmentService";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RabbitMqOptions _options = options.Value;
    
    public async Task PublishAsync<TPayload>(
        string eventType,
        string routingKey,
        TPayload payload,
        string? correlationId,
        CancellationToken ct)
    {
        var envelope = new IntegrationEventEnvelope<TPayload>
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            EventVersion = 1,
            OccurredAt = DateTime.UtcNow,
            CorrelationId = correlationId,
            Source = EventSource,
            Payload = payload
        };

        var json = JsonSerializer.Serialize(envelope, JsonOptions);
        var body = Encoding.UTF8.GetBytes(json);

        var rabbitConnection = await connection.GetConnectionAsync(ct);
        await using var channel = await rabbitConnection.CreateChannelAsync(cancellationToken: ct);

        await channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = envelope.EventId.ToString(),
            Type = eventType
        };

        await channel.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);

        logger.LogInformation(
            "Integration event published. EventId={EventId}, EventType={EventType}, RoutingKey={RoutingKey}," +
            " CorrelationId={CorrelationId}, Exchange={Exchange}",
            envelope.EventId,
            eventType,
            routingKey,
            correlationId,
            _options.ExchangeName);
    }
}
