namespace NotificationService.Common.Messaging;

public readonly record struct NotificationDeliveryDecision(bool DeadLetter, string? Reason)
{
    public const int SupportedEventVersion = 1;

    public static NotificationDeliveryDecision Ack { get; } = new(false, null);

    public static NotificationDeliveryDecision Evaluate(
        bool hasPayload,
        string? eventType,
        string expectedEventType,
        int eventVersion)
    {
        if (!hasPayload)
            return new NotificationDeliveryDecision(true, "empty-payload");

        if (!string.Equals(eventType, expectedEventType, StringComparison.Ordinal))
            return new NotificationDeliveryDecision(true, "unexpected-event-type");

        if (eventVersion != SupportedEventVersion)
            return new NotificationDeliveryDecision(true, "unsupported-event-version");

        return Ack;
    }
}
