using MedConnect.Shared.Events;
using NotificationService.Common.DependencyInjection;
using NotificationService.Common.Messaging;
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

        services.AddSingleton<INotificationEventHandler<AppointmentCreatedPayload>, AppointmentCreatedHandler>();
        services.AddSingleton<INotificationEventHandler<AppointmentCancelledPayload>, AppointmentCancelledHandler>();
        services.AddSingleton<INotificationEventHandler<MessageCreatedPayload>, MessageCreatedHandler>();
    }
}
