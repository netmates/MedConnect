using MongoDB.Driver;

namespace CommunicationService.Common.Persistence;

public static class MongoConsumerRetry
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    private static readonly TimeSpan[] Delays =
    [
        TimeSpan.Zero,
        RetryDelay
    ];

    private static bool IsTransientMongo(Exception ex) =>
        ex is MongoException or TimeoutException;

    public static async Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        ILogger logger,
        CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await action(ct);
                return;
            }
            catch (Exception ex) when (!IsTransientMongo(ex))
            {
                throw;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Transient Mongo failure, retry {Attempt}/{MaxAttempts}",
                    attempt,
                    MaxAttempts);

                await Task.Delay(Delays[attempt - 1], ct);
            }
        }
    }
}
