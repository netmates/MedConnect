namespace NotificationService.Common.Messaging;

public sealed record NotificationSubscription(string QueueName, string RoutingKey, string EventType)
{
    private const string QueueSuffix = ".q";
    private const string DeadLetterSuffix = ".dlq";

    public string DeadLetterQueueName =>
        QueueName.EndsWith(QueueSuffix, StringComparison.Ordinal)
            ? string.Concat(QueueName.AsSpan(0, QueueName.Length - QueueSuffix.Length), DeadLetterSuffix)
            : QueueName + DeadLetterSuffix;
}
