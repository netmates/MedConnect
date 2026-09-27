using System.Text;
using System.Text.Json;
using MedConnect.Shared.Events;
using NotificationService.Common.Messaging;

namespace NotificationService.UnitTests.Messaging;

public class NotificationMessageProcessorTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public async Task ValidEnvelope_IsAcknowledged_AndHandlerRuns()
    {
        var appointmentId = Guid.NewGuid();
        var handled = new List<Guid>();

        var result = await NotificationMessageProcessor.ProcessAsync<AppointmentCreatedPayload>(
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

        Assert.False(result.DeadLetter);
        Assert.False(result.Requeue);
        Assert.Equal([appointmentId], handled);
    }

    [Theory]
    [InlineData("{", "invalid-json")]
    [InlineData("null", "invalid-json")]
    public async Task InvalidJson_IsDeadLetteredWithoutRequeue(string json, string reason)
    {
        var handled = false;

        var result = await NotificationMessageProcessor.ProcessAsync<AppointmentCreatedPayload>(
            Encoding.UTF8.GetBytes(json),
            EventTypes.AppointmentCreated,
            (_, _) =>
            {
                handled = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.True(result.DeadLetter);
        Assert.False(result.Requeue);
        Assert.Equal(reason, result.Reason);
        Assert.False(handled);
    }

    [Fact]
    public async Task UnexpectedEventType_IsDeadLetteredWithoutRequeue()
    {
        var result = await ProcessAsync(
            EventTypes.MessageCreated,
            eventVersion: 1,
            payload: new AppointmentCreatedPayload());

        Assert.True(result.DeadLetter);
        Assert.False(result.Requeue);
        Assert.Equal("unexpected-event-type", result.Reason);
    }

    [Fact]
    public async Task UnsupportedEventVersion_IsDeadLetteredWithoutRequeue()
    {
        var result = await ProcessAsync(
            EventTypes.AppointmentCreated,
            eventVersion: 2,
            payload: new AppointmentCreatedPayload());

        Assert.True(result.DeadLetter);
        Assert.False(result.Requeue);
        Assert.Equal("unsupported-event-version", result.Reason);
    }

    [Fact]
    public async Task EmptyPayload_IsDeadLetteredWithoutRequeue()
    {
        var json = """
            {
              "eventId": "3f6f72f7-3997-4d1a-9c0c-6f62cf57ec0d",
              "eventType": "AppointmentCreated",
              "eventVersion": 1,
              "payload": null
            }
            """;

        var result = await NotificationMessageProcessor.ProcessAsync<AppointmentCreatedPayload>(
            Encoding.UTF8.GetBytes(json),
            EventTypes.AppointmentCreated,
            (_, _) => Task.CompletedTask,
            CancellationToken.None);

        Assert.True(result.DeadLetter);
        Assert.False(result.Requeue);
        Assert.Equal("empty-payload", result.Reason);
    }

    [Fact]
    public async Task HandlerException_IsDeadLetteredWithoutRequeue()
    {
        var result = await NotificationMessageProcessor.ProcessAsync<AppointmentCreatedPayload>(
            Envelope(EventTypes.AppointmentCreated, 1, new AppointmentCreatedPayload()),
            EventTypes.AppointmentCreated,
            (_, _) => throw new InvalidOperationException("sender failed"),
            CancellationToken.None);

        Assert.True(result.DeadLetter);
        Assert.False(result.Requeue);
        Assert.Equal("processing-failed", result.Reason);
        Assert.IsType<InvalidOperationException>(result.Error);
    }

    private static Task<NotificationDeliveryResult> ProcessAsync(
        string eventType,
        int eventVersion,
        AppointmentCreatedPayload payload) =>
        NotificationMessageProcessor.ProcessAsync<AppointmentCreatedPayload>(
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
