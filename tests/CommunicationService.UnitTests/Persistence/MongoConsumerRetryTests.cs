using CommunicationService.Common.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace CommunicationService.UnitTests.Persistence;

public class MongoConsumerRetryTests
{
    [Fact]
    public async Task ExecuteAsync_WhenActionSucceeds_RunsOnce()
    {
        // Arrange
        var calls = 0;

        // Act
        await MongoConsumerRetry.ExecuteAsync(
            _ =>
            {
                calls++;
                return Task.CompletedTask;
            },
            NullLogger.Instance,
            CancellationToken.None);

        // Assert
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMongoFailsOnce_Retries()
    {
        // Arrange
        var calls = 0;

        // Act
        await MongoConsumerRetry.ExecuteAsync(
            _ =>
            {
                calls++;
                if (calls == 1)
                    throw new MongoException("temporary");

                return Task.CompletedTask;
            },
            NullLogger.Instance,
            CancellationToken.None);

        // Assert
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFailureIsNotTransient_DoesNotRetry()
    {
        // Arrange
        var calls = 0;

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            MongoConsumerRetry.ExecuteAsync(
                _ =>
                {
                    calls++;
                    throw new InvalidOperationException("permanent");
                },
                NullLogger.Instance,
                CancellationToken.None));

        // Assert
        Assert.Equal("permanent", ex.Message);
        Assert.Equal(1, calls);
    }
}
