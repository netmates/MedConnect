using MedConnect.Messaging;
using MedConnect.Shared.Events;
using Microsoft.Extensions.Options;

namespace NotificationService.Common.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddNotificationConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRabbitMq(configuration, "NotificationService");

        services
            .AddOptions<NotificationQueueOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHostedService(sp => CreateConsumer(
            sp,
            options => new NotificationSubscription(
                options.AppointmentCreatedQueue,
                RoutingKeys.AppointmentCreated,
                EventTypes.AppointmentCreated)));

        services.AddHostedService(sp => CreateConsumer(
            sp,
            options => new NotificationSubscription(
                options.AppointmentCancelledQueue,
                RoutingKeys.AppointmentCancelled,
                EventTypes.AppointmentCancelled)));

        services.AddHostedService(sp => CreateConsumer(
            sp,
            options => new NotificationSubscription(
                options.MessageCreatedQueue,
                RoutingKeys.MessageCreated,
                EventTypes.MessageCreated)));

        return services;
    }

    private static NotificationQueueConsumer CreateConsumer(
        IServiceProvider serviceProvider,
        Func<NotificationQueueOptions, NotificationSubscription> subscription)
    {
        var queues = serviceProvider.GetRequiredService<IOptions<NotificationQueueOptions>>().Value;

        return new NotificationQueueConsumer(
            serviceProvider.GetRequiredService<RabbitMqConnection>(),
            serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>(),
            serviceProvider.GetRequiredService<IOptions<NotificationQueueOptions>>(),
            subscription(queues),
            serviceProvider.GetRequiredService<ILogger<NotificationQueueConsumer>>());
    }
}
