namespace MedConnect.Shared.Consuming;

public interface IConsumerQueueSettings
{
    public string DeadLetterExchangeName { get; }

    public ushort PrefetchCount { get; }
}
