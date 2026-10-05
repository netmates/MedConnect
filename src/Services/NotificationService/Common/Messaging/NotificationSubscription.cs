namespace NotificationService.Common.Messaging;

public sealed record NotificationSubscription(string QueueName, string RoutingKey, string EventType)
{
    public string DeadLetterQueueName =>
        QueueName.EndsWith(".q", StringComparison.Ordinal)
            ? string.Concat(QueueName.AsSpan(0, QueueName.Length - 2), ".dlq")
            : QueueName + ".dlq";
}
