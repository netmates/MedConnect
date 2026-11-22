using MedConnect.Shared.Consuming;
using NotificationService.Common.DependencyInjection;
using NotificationService.Features.Notifications.Handlers;
using NotificationService.Features.Notifications.Senders;
using Scrutor;

namespace NotificationService.Features.Notifications;

public sealed class NotificationModule : IServiceModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<NotificationOptions>()
            .Bind(configuration.GetSection(NotificationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddKeyedSingleton<INotificationSender, FakeNotificationSender>(NotificationChannels.Fake);
        services.AddKeyedSingleton<INotificationSender, EmailNotificationSender>(NotificationChannels.Email);
        services.AddKeyedSingleton<INotificationSender, SmsNotificationSender>(NotificationChannels.Sms);
        services.AddSingleton<INotificationSender, ConfiguredNotificationSender>();

        services.Scan(scan => scan
            .FromAssemblyOf<AppointmentCreatedHandler>()
            .AddClasses(classes => classes
                .InNamespaceOf<AppointmentCreatedHandler>()
                .AssignableTo(typeof(IIntegrationEventHandler<>)))
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        services.Decorate(typeof(IIntegrationEventHandler<>), typeof(LoggingIntegrationEventHandler<>));
    }
}
