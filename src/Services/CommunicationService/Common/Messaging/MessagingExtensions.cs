using MedConnect.Shared.Messaging;
using MedConnect.Shared.Events;
using MedConnect.Shared.Consuming;
using Microsoft.Extensions.Options;

namespace CommunicationService.Common.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddCommunicationConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRabbitMq(configuration, "CommunicationService");

        services
            .AddOptions<CommunicationQueueOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHostedService(sp => CreateConsumer<AppointmentCreatedPayload>(
            sp,
            options => new QueueSubscription(
                options.AppointmentCreatedQueue,
                RoutingKeys.AppointmentCreated,
                EventTypes.AppointmentCreated)));

        services.AddHostedService(sp => CreateConsumer<ParticipantNameUpdatedPayload>(
            sp,
            options => new QueueSubscription(
                options.ParticipantNameUpdatedQueue,
                RoutingKeys.ParticipantNameUpdated,
                EventTypes.ParticipantNameUpdated)));

        return services;
    }

    private static QueueConsumer<TPayload> CreateConsumer<TPayload>(
        IServiceProvider serviceProvider,
        Func<CommunicationQueueOptions, QueueSubscription> subscription)
    {
        var queues = serviceProvider.GetRequiredService<IOptions<CommunicationQueueOptions>>().Value;

        return new QueueConsumer<TPayload>(
            serviceProvider.GetRequiredService<RabbitMqConnection>(),
            serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>(),
            queues,
            subscription(queues),
            serviceProvider.GetRequiredService<IIntegrationEventHandler<TPayload>>(),
            serviceProvider.GetRequiredService<ILogger<QueueConsumer<TPayload>>>());
    }
}
