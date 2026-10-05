using MedConnect.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Context;

namespace CommunicationService.Common.Messaging;

public sealed class CommunicationQueueConsumer<TPayload>(
    RabbitMqConnection connection,
    IOptions<RabbitMqOptions> rabbitOptions,
    IOptions<CommunicationQueueOptions> queueOptions,
    CommunicationSubscription subscription,
    ICommunicationEventHandler<TPayload> handler,
    ILogger<CommunicationQueueConsumer<TPayload>> logger) : BackgroundService
{
    private readonly RabbitMqOptions _rabbitOptions = rabbitOptions.Value;
    private readonly CommunicationQueueOptions _queueOptions = queueOptions.Value;
    private readonly SemaphoreSlim _channelGate = new(1, 1);
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitConnection = await connection.GetConnectionAsync(stoppingToken);
        _channel = await rabbitConnection.CreateChannelAsync(cancellationToken: stoppingToken);

        await DeclareTopologyAsync(stoppingToken);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: _queueOptions.PrefetchCount,
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
            "Communication consumer started. Queue={Queue}, RoutingKey={RoutingKey}, DeadLetterQueue={DeadLetterQueue}",
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
        var result = await CommunicationMessageProcessor.ProcessAsync<TPayload>(
            ea.Body,
            subscription.EventType,
            handler.HandleAsync,
            CancellationToken.None);

        if (result.DeadLetter)
        {
            LogDeadLetter(result, ea.DeliveryTag);
            await NackAsync(ea.DeliveryTag, result.Requeue);
            return;
        }

        using (LogContext.PushProperty("CorrelationId", result.CorrelationId))
        using (LogContext.PushProperty("EventId", result.EventId))
        {
            logger.LogInformation(
                "Integration event was consumed. EventId={EventId}, EventType={EventType}, Queue={Queue}, CorrelationId={CorrelationId}",
                result.EventId,
                result.EventType,
                subscription.QueueName,
                result.CorrelationId);
        }

        await AckAsync(ea.DeliveryTag);
    }

    private void LogDeadLetter(CommunicationDeliveryResult result, ulong deliveryTag)
    {
        using var correlationScope = result.CorrelationId is null
            ? null
            : LogContext.PushProperty("CorrelationId", result.CorrelationId);
        using var eventScope = result.EventId == Guid.Empty
            ? null
            : LogContext.PushProperty("EventId", result.EventId);

        if (result.Error is null)
        {
            logger.LogError(
                "Integration event was dead-lettered. Reason={Reason}, EventId={EventId}, EventType={EventType}, EventVersion={EventVersion}, Queue={Queue}, DeliveryTag={DeliveryTag}",
                result.Reason,
                result.EventId,
                result.EventType,
                result.EventVersion,
                subscription.QueueName,
                deliveryTag);
            return;
        }

        logger.LogError(
            result.Error,
            "Integration event was dead-lettered. Reason={Reason}, EventId={EventId}, EventType={EventType}, EventVersion={EventVersion}, Queue={Queue}, DeliveryTag={DeliveryTag}",
            result.Reason,
            result.EventId,
            result.EventType,
            result.EventVersion,
            subscription.QueueName,
            deliveryTag);
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

    private async Task NackAsync(ulong deliveryTag, bool requeue)
    {
        await _channelGate.WaitAsync(CancellationToken.None);
        try
        {
            await Channel.BasicNackAsync(deliveryTag, multiple: false, requeue: requeue);
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
