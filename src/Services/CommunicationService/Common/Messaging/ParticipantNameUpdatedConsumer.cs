using System.Text;
using System.Text.Json;
using CommunicationService.Features.Chats;
using MedConnect.Shared.Events;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Context;

namespace CommunicationService.Common.Messaging;

public sealed class ParticipantNameUpdatedConsumer(
    RabbitMqConnection connection,
    IOptions<RabbitMqOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<ParticipantNameUpdatedConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly RabbitMqOptions _options = options.Value;
    private IChannel? _channel;
    private CancellationToken _stoppingToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        var rabbitConnection = await connection.GetConnectionAsync(stoppingToken);
        _channel = await rabbitConnection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: _options.ParticipantNameUpdatedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: _options.ParticipantNameUpdatedQueue,
            exchange: _options.ExchangeName,
            routingKey: RoutingKeys.ParticipantNameUpdated,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: _options.ParticipantNameUpdatedQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation(
            "ParticipantNameUpdated consumer started. Queue={Queue}, RoutingKey={RoutingKey}",
            _options.ParticipantNameUpdatedQueue,
            RoutingKeys.ParticipantNameUpdated);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var channel = _channel
            ?? throw new InvalidOperationException("RabbitMQ channel is not initialized.");

        IntegrationEventEnvelope<ParticipantNameUpdatedPayload>? envelope;
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope<ParticipantNameUpdatedPayload>>(
                json, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize ParticipantNameUpdated. DeliveryTag={DeliveryTag}", ea.DeliveryTag);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        if (envelope?.Payload is null)
        {
            logger.LogError("ParticipantNameUpdated envelope/payload is null. DeliveryTag={DeliveryTag}", ea.DeliveryTag);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        if (!string.Equals(envelope.EventType, EventTypes.ParticipantNameUpdated, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Unexpected event type on participant-name-updated queue. EventType={EventType}, EventId={EventId}",
                envelope.EventType, envelope.EventId);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            return;
        }

        var payload = envelope.Payload;

        using (LogContext.PushProperty("CorrelationId", envelope.CorrelationId))
        using (LogContext.PushProperty("EventId", envelope.EventId))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var updater = scope.ServiceProvider.GetRequiredService<UpdateChatParticipantNamesService>();

                long modified;
                if (string.Equals(payload.Role, ParticipantRoles.Patient, StringComparison.Ordinal))
                {
                    modified = await updater.UpdatePatientNameAsync(
                        payload.ParticipantId,
                        payload.FullName,
                        _stoppingToken);
                }
                else if (string.Equals(payload.Role, ParticipantRoles.Doctor, StringComparison.Ordinal))
                {
                    modified = await updater.UpdateDoctorNameAsync(
                        payload.ParticipantId,
                        payload.FullName,
                        _stoppingToken);
                }
                else
                {
                    logger.LogWarning(
                        "Unknown participant role. Role={Role}, ParticipantId={ParticipantId}, EventId={EventId}",
                        payload.Role, payload.ParticipantId, envelope.EventId);
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                logger.LogInformation(
                    "ParticipantNameUpdated processed. Role={Role}, ParticipantId={ParticipantId}, ModifiedChats={ModifiedChats}, EventId={EventId}",
                    payload.Role, payload.ParticipantId, modified, envelope.EventId);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to process ParticipantNameUpdated. Role={Role}, ParticipantId={ParticipantId}, EventId={EventId}",
                    payload.Role, payload.ParticipantId, envelope.EventId);

                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        }
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        await base.StopAsync(ct);

        if (_channel is not null)
        {
            await _channel.CloseAsync(CancellationToken.None);
            await _channel.DisposeAsync();
            _channel = null;
        }
    }
}
