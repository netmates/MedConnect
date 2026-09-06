using System.Text;
using System.Text.Json;
using CommunicationService.Features.Chats;
using MedConnect.Shared.Events;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Context;

namespace CommunicationService.Common.Messaging;

public sealed class AppointmentCreatedConsumer(
    RabbitMqConnection connection,
    IOptions<RabbitMqOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<AppointmentCreatedConsumer> logger) : BackgroundService
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
            queue: _options.AppointmentCreatedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: _options.AppointmentCreatedQueue,
            exchange: _options.ExchangeName,
            routingKey: RoutingKeys.AppointmentCreated,
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
            queue: _options.AppointmentCreatedQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation(
            "AppointmentCreated consumer started. Queue={Queue}, RoutingKey={RoutingKey}",
            _options.AppointmentCreatedQueue,
            RoutingKeys.AppointmentCreated);

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

        IntegrationEventEnvelope<AppointmentCreatedPayload>? envelope;
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope<AppointmentCreatedPayload>>(
                json, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize AppointmentCreated. DeliveryTag={DeliveryTag}", ea.DeliveryTag);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        if (envelope?.Payload is null)
        {
            logger.LogError("AppointmentCreated envelope/payload is null. DeliveryTag={DeliveryTag}", ea.DeliveryTag);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        if (!string.Equals(envelope.EventType, EventTypes.AppointmentCreated, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Unexpected event type on appointment-created queue. EventType={EventType}, EventId={EventId}",
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
                var ensureChat = scope.ServiceProvider.GetRequiredService<EnsureChatService>();

                var (_, created) = await ensureChat.EnsureAsync(
                    payload.AppointmentId,
                    payload.PatientId,
                    payload.DoctorId,
                    payload.PatientKeycloakId,
                    payload.DoctorKeycloakId,
                    payload.PatientName,
                    payload.DoctorName,
                    _stoppingToken);

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                logger.LogInformation(
                    "AppointmentCreated processed. AppointmentId={AppointmentId}, ChatCreated={ChatCreated}, EventId={EventId}",
                    payload.AppointmentId, created, envelope.EventId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to process AppointmentCreated. AppointmentId={AppointmentId}, EventId={EventId}",
                    payload.AppointmentId, envelope.EventId);

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

        await connection.DisposeAsync();
    }
}
