using System.Text;
using System.Text.Json;
using MedConnect.Shared.Consuming;
using MedConnect.Shared.Events;

namespace CommunicationService.UnitTests.Messaging;

public class QueueMessageProcessorTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public async Task ValidEnvelope_IsAcknowledged_AndHandlerRuns()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var handled = new List<Guid>();

        // Act
        var result = await QueueMessageProcessor.ProcessAsync<AppointmentCreatedPayload>(
            Envelope(EventTypes.AppointmentCreated, 1, new AppointmentCreatedPayload
            {
                AppointmentId = appointmentId
            }),
            EventTypes.AppointmentCreated,
            (payload, _) =>
            {
                handled.Add(payload.AppointmentId);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        // Assert
        Assert.False(result.DeadLetter);
        Assert.Equal([appointmentId], handled);
    }

    [Theory]
    [InlineData("{", "invalid-json")]
    [InlineData("null", "invalid-json")]
    public async Task InvalidJson_IsDeadLettered(string json, string reason)
    {
        // Arrange
        var handled = false;

        // Act
        var result = await QueueMessageProcessor.ProcessAsync<AppointmentCreatedPayload>(
            Encoding.UTF8.GetBytes(json),
            EventTypes.AppointmentCreated,
            (_, _) =>
            {
                handled = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        // Assert
        Assert.True(result.DeadLetter);
        Assert.Equal(reason, result.Reason);
        Assert.False(handled);
    }

    [Fact]
    public async Task UnexpectedEventType_IsDeadLettered()
    {
        // Arrange
        var payload = new AppointmentCreatedPayload();

        // Act
        var result = await ProcessAsync(
            EventTypes.MessageCreated,
            eventVersion: 1,
            payload);

        // Assert
        Assert.True(result.DeadLetter);
        Assert.Equal("unexpected-event-type", result.Reason);
    }

    [Fact]
    public async Task UnsupportedEventVersion_IsDeadLettered()
    {
        // Arrange
        var payload = new AppointmentCreatedPayload();

        // Act
        var result = await ProcessAsync(
            EventTypes.AppointmentCreated,
            eventVersion: 2,
            payload);

        // Assert
        Assert.True(result.DeadLetter);
        Assert.Equal("unsupported-event-version", result.Reason);
    }

    [Fact]
    public async Task EmptyPayload_IsDeadLettered()
    {
        // Arrange
        var json = """
            {
              "eventId": "3f6f72f7-3997-4d1a-9c0c-6f62cf57ec0d",
              "eventType": "AppointmentCreated",
              "eventVersion": 1,
              "payload": null
            }
            """;

        // Act
        var result = await QueueMessageProcessor.ProcessAsync<AppointmentCreatedPayload>(
            Encoding.UTF8.GetBytes(json),
            EventTypes.AppointmentCreated,
            (_, _) => Task.CompletedTask,
            CancellationToken.None);

        // Assert
        Assert.True(result.DeadLetter);
        Assert.Equal("empty-payload", result.Reason);
    }

    [Fact]
    public async Task HandlerException_IsDeadLettered()
    {
        // Arrange
        var body = Envelope(EventTypes.AppointmentCreated, 1, new AppointmentCreatedPayload());

        // Act
        var result = await QueueMessageProcessor.ProcessAsync<AppointmentCreatedPayload>(
            body,
            EventTypes.AppointmentCreated,
            (_, _) => throw new InvalidOperationException("handler failed"),
            CancellationToken.None);

        // Assert
        Assert.True(result.DeadLetter);
        Assert.Equal("processing-failed", result.Reason);
        Assert.IsType<InvalidOperationException>(result.Error);
    }

    private static Task<DeliveryResult> ProcessAsync(
        string eventType,
        int eventVersion,
        AppointmentCreatedPayload payload) =>
        QueueMessageProcessor.ProcessAsync<AppointmentCreatedPayload>(
            Envelope(eventType, eventVersion, payload),
            EventTypes.AppointmentCreated,
            (_, _) => Task.CompletedTask,
            CancellationToken.None);

    private static byte[] Envelope<TPayload>(string eventType, int eventVersion, TPayload payload)
    {
        var envelope = new IntegrationEventEnvelope<TPayload>
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            EventVersion = eventVersion,
            OccurredAt = DateTime.UtcNow,
            CorrelationId = "corr-1",
            Source = "tests",
            Payload = payload
        };

        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope, JsonOptions));
    }
}
