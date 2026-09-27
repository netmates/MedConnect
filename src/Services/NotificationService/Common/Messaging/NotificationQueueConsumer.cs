using System.Text;
using System.Text.Json;
using MedConnect.Messaging;
using MedConnect.Shared.Events;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Context;

namespace NotificationService.Common.Messaging;

public sealed class NotificationQueueConsumer<TPayload>(
    RabbitMqConnection connection,
    IOptions<RabbitMqOptions> rabbitOptions,
    IOptions<NotificationQueueOptions> queueOptions,
    NotificationSubscription subscription,
    INotificationHandler<TPayload> handler,
    ILogger<NotificationQueueConsumer<TPayload>> logger) : BackgroundService
{
    private const ushort PrefetchCount = 10;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly RabbitMqOptions _rabbitOptions = rabbitOptions.Value;
    private readonly NotificationQueueOptions _queueOptions = queueOptions.Value;
    private readonly SemaphoreSlim _channelGate = new(1, 1);
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitConnection = await connection.GetConnectionAsync(stoppingToken);
        _channel = await rabbitConnection.CreateChannelAsync(cancellationToken: stoppingToken);

        await DeclareTopologyAsync(stoppingToken);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: PrefetchCount,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: subscription.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation(
            "Notification consumer started. Queue={Queue}, RoutingKey={RoutingKey}, DeadLetterQueue={DeadLetterQueue}",
            subscription.QueueName,
            subscription.RoutingKey,
            subscription.DeadLetterQueueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
    }

    private async Task DeclareTopologyAsync(CancellationToken ct)
    {
        var channel = Channel;

        await channel.ExchangeDeclareAsync(
            exchange: _rabbitOptions.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        await channel.ExchangeDeclareAsync(
            exchange: _queueOptions.DeadLetterExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        await channel.QueueDeclareAsync(
            queue: subscription.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: subscription.DeadLetterQueueName,
            exchange: _queueOptions.DeadLetterExchangeName,
            routingKey: subscription.DeadLetterQueueName,
            arguments: null,
            cancellationToken: ct);

        var queueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = _queueOptions.DeadLetterExchangeName,
            ["x-dead-letter-routing-key"] = subscription.DeadLetterQueueName
        };

        await channel.QueueDeclareAsync(
            queue: subscription.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: ct);

        await channel.QueueBindAsync(
            queue: subscription.QueueName,
            exchange: _rabbitOptions.ExchangeName,
            routingKey: subscription.RoutingKey,
            arguments: null,
            cancellationToken: ct);
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        IntegrationEventEnvelope<JsonElement>? envelope;
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope<JsonElement>>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Integration event was dead-lettered. Reason={Reason}, Queue={Queue}, DeliveryTag={DeliveryTag}",
                "invalid-json",
                subscription.QueueName,
                ea.DeliveryTag);
            await NackAsync(ea.DeliveryTag);
            return;
        }

        if (envelope is null)
        {
            logger.LogError(
                "Integration event was dead-lettered. Reason={Reason}, Queue={Queue}, DeliveryTag={DeliveryTag}",
                "invalid-json",
                subscription.QueueName,
                ea.DeliveryTag);
            await NackAsync(ea.DeliveryTag);
            return;
        }

        var hasPayload = envelope.Payload.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null;
        var decision = NotificationDeliveryDecision.Evaluate(
            hasPayload,
            envelope.EventType,
            subscription.EventType,
            envelope.EventVersion);

        if (decision.DeadLetter)
        {
            logger.LogError(
                "Integration event was dead-lettered. Reason={Reason}, EventId={EventId}, EventType={EventType}, EventVersion={EventVersion}, Queue={Queue}",
                decision.Reason,
                envelope.EventId,
                envelope.EventType,
                envelope.EventVersion,
                subscription.QueueName);
            await NackAsync(ea.DeliveryTag);
            return;
        }

        TPayload? payload;
        try
        {
            payload = envelope.Payload.Deserialize<TPayload>(JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(
                ex,
                "Integration event was dead-lettered. Reason={Reason}, EventId={EventId}, EventType={EventType}, Queue={Queue}",
                "invalid-payload",
                envelope.EventId,
                envelope.EventType,
                subscription.QueueName);
            await NackAsync(ea.DeliveryTag);
            return;
        }

        if (payload is null)
        {
            logger.LogError(
                "Integration event was dead-lettered. Reason={Reason}, EventId={EventId}, EventType={EventType}, Queue={Queue}",
                "empty-payload",
                envelope.EventId,
                envelope.EventType,
                subscription.QueueName);
            await NackAsync(ea.DeliveryTag);
            return;
        }

        using (LogContext.PushProperty("CorrelationId", envelope.CorrelationId))
        using (LogContext.PushProperty("EventId", envelope.EventId))
        {
            try
            {
                await handler.HandleAsync(payload, CancellationToken.None);

                logger.LogInformation(
                    "Integration event was consumed. EventId={EventId}, EventType={EventType}, Queue={Queue}, CorrelationId={CorrelationId}",
                    envelope.EventId,
                    envelope.EventType,
                    subscription.QueueName,
                    envelope.CorrelationId);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Integration event was dead-lettered. Reason={Reason}, EventId={EventId}, EventType={EventType}, Queue={Queue}",
                    "processing-failed",
                    envelope.EventId,
                    envelope.EventType,
                    subscription.QueueName);
                await NackAsync(ea.DeliveryTag);
                return;
            }
        }

        await AckAsync(ea.DeliveryTag);
    }

    private IChannel Channel =>
        _channel ?? throw new InvalidOperationException("RabbitMQ channel is not initialized.");

    private async Task AckAsync(ulong deliveryTag)
    {
        await _channelGate.WaitAsync(CancellationToken.None);
        try
        {
            await Channel.BasicAckAsync(deliveryTag, multiple: false);
        }
        finally
        {
            _channelGate.Release();
        }
    }

    private async Task NackAsync(ulong deliveryTag)
    {
        await _channelGate.WaitAsync(CancellationToken.None);
        try
        {
            await Channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false);
        }
        finally
        {
            _channelGate.Release();
        }
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        await base.StopAsync(ct);

        if (_channel is null)
            return;

        await _channel.CloseAsync(CancellationToken.None);
        await _channel.DisposeAsync();
        _channel = null;
    }
}
