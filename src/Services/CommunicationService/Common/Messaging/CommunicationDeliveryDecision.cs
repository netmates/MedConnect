namespace CommunicationService.Common.Messaging;

public readonly record struct CommunicationDeliveryDecision(bool DeadLetter, string? Reason)
{
    public const int SupportedEventVersion = 1;

    public static CommunicationDeliveryDecision Ack { get; } = new(false, null);

    public static CommunicationDeliveryDecision Evaluate(
        bool hasPayload,
        string? eventType,
        string expectedEventType,
        int eventVersion)
    {
        if (!hasPayload)
            return new CommunicationDeliveryDecision(true, "empty-payload");

        if (!string.Equals(eventType, expectedEventType, StringComparison.Ordinal))
            return new CommunicationDeliveryDecision(true, "unexpected-event-type");

        if (eventVersion != SupportedEventVersion)
            return new CommunicationDeliveryDecision(true, "unsupported-event-version");

        return Ack;
    }
}
