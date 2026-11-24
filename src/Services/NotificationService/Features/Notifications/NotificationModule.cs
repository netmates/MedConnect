using MedConnect.Shared.Consuming;
using NotificationService.Common.DependencyInjection;
using NotificationService.Features.Notifications.Handlers;
using NotificationService.Features.Notifications.Senders;

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

        // Обработчики из пространства имен Handlers, реализующие IIntegrationEventHandler<>. Один экземпляр на процесс
        services.Scan(scan => scan
            .FromAssemblyOf<AppointmentCreatedHandler>()
            .AddClasses(classes => classes
                .InNamespaceOf<AppointmentCreatedHandler>()
                .AssignableTo(typeof(IIntegrationEventHandler<>)))
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        // Контейнер отдает обертку LoggingIntegrationEventHandler: она пишет лог и вызывает исходный обработчик
        services.Decorate(typeof(IIntegrationEventHandler<>), typeof(LoggingIntegrationEventHandler<>));
    }
}
