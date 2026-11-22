using CommunicationService.Common.Consuming;
using MedConnect.Shared.Consuming;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace CommunicationService.UnitTests.Consuming;

public class MongoRetryIntegrationEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenMongoFailsOnce_RetriesInner()
    {
        // Arrange
        var inner = new FakeHandler
        {
            FailWith = new MongoException("temporary"),
            FailTimes = 1
        };
        var handler = CreateHandler(inner);

        // Act
        await handler.HandleAsync("payload", CancellationToken.None);

        // Assert
        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task HandleAsync_WhenFailureIsNotTransient_DoesNotRetry()
    {
        // Arrange
        var inner = new FakeHandler
        {
            FailWith = new InvalidOperationException("permanent"),
            FailTimes = 1
        };
        var handler = CreateHandler(inner);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync("payload", CancellationToken.None));

        // Assert
        Assert.Equal("permanent", ex.Message);
        Assert.Equal(1, inner.Calls);
    }

    private static MongoRetryIntegrationEventHandler<string> CreateHandler(FakeHandler inner) =>
        new(inner, NullLogger<MongoRetryIntegrationEventHandler<string>>.Instance);

    private sealed class FakeHandler : IIntegrationEventHandler<string>
    {
        public int Calls { get; private set; }
        public Exception? FailWith { get; init; }
        public int FailTimes { get; init; }

        public Task HandleAsync(string payload, CancellationToken ct)
        {
            Calls++;
            if (FailWith is not null && Calls <= FailTimes)
                throw FailWith;

            return Task.CompletedTask;
        }
    }
}
