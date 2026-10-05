using MongoDB.Driver;

namespace CommunicationService.Common.Persistence;

public static class MongoConsumerRetry
{
    public const int MaxAttempts = 3;

    private static readonly TimeSpan[] Delays =
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
    ];

    public static bool IsTransientMongo(Exception ex) =>
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
