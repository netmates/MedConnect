using MedConnect.Shared.Consuming;
using MedConnect.Shared.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Features.Notifications;

namespace NotificationService.UnitTests.DependencyInjection;

public class NotificationModuleTests
{
    [Fact]
    public void Register_ResolvesLoggingDecorator()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{NotificationOptions.SectionName}:Channel"] = NotificationChannels.Fake
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        new NotificationModule().Register(services, configuration);

        using var provider = services.BuildServiceProvider();

        var created = provider.GetRequiredService<IIntegrationEventHandler<AppointmentCreatedPayload>>();
        var cancelled = provider.GetRequiredService<IIntegrationEventHandler<AppointmentCancelledPayload>>();
        var message = provider.GetRequiredService<IIntegrationEventHandler<MessageCreatedPayload>>();

        Assert.IsType<LoggingIntegrationEventHandler<AppointmentCreatedPayload>>(created);
        Assert.IsType<LoggingIntegrationEventHandler<AppointmentCancelledPayload>>(cancelled);
        Assert.IsType<LoggingIntegrationEventHandler<MessageCreatedPayload>>(message);
    }
}
