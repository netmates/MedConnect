using MedConnect.Shared.Messaging;
using MedConnect.Shared.Events;
using MedConnect.Shared.Consuming;
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

        services.AddHostedService(sp => CreateConsumer<AppointmentCreatedPayload>(
            sp,
            options => new QueueSubscription(
                options.AppointmentCreatedQueue,
                RoutingKeys.AppointmentCreated,
                EventTypes.AppointmentCreated)));

        services.AddHostedService(sp => CreateConsumer<AppointmentCancelledPayload>(
            sp,
            options => new QueueSubscription(
                options.AppointmentCancelledQueue,
                RoutingKeys.AppointmentCancelled,
                EventTypes.AppointmentCancelled)));

        services.AddHostedService(sp => CreateConsumer<MessageCreatedPayload>(
            sp,
            options => new QueueSubscription(
                options.MessageCreatedQueue,
                RoutingKeys.MessageCreated,
                EventTypes.MessageCreated)));

        return services;
    }

    private static QueueConsumer<TPayload> CreateConsumer<TPayload>(
        IServiceProvider serviceProvider,
        Func<NotificationQueueOptions, QueueSubscription> subscription)
    {
        var queues = serviceProvider.GetRequiredService<IOptions<NotificationQueueOptions>>().Value;

        return new QueueConsumer<TPayload>(
            serviceProvider.GetRequiredService<RabbitMqConnection>(),
            serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>(),
            queues,
            subscription(queues),
            serviceProvider.GetRequiredService<IIntegrationEventHandler<TPayload>>(),
            serviceProvider.GetRequiredService<ILogger<QueueConsumer<TPayload>>>());
    }
}
