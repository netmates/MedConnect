using MedConnect.Shared.Events;
using NotificationService.Common.DependencyInjection;
using NotificationService.Common.Messaging;

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

        services.AddSingleton<INotificationHandler<AppointmentCreatedPayload>, AppointmentCreatedHandler>();
        services.AddSingleton<INotificationHandler<AppointmentCancelledPayload>, AppointmentCancelledHandler>();
        services.AddSingleton<INotificationHandler<MessageCreatedPayload>, MessageCreatedHandler>();
    }
}
