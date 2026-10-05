using MedConnect.Messaging;
using MedConnect.Shared.Events;
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
            options => new CommunicationSubscription(
                options.AppointmentCreatedQueue,
                RoutingKeys.AppointmentCreated,
                EventTypes.AppointmentCreated)));

        services.AddHostedService(sp => CreateConsumer<ParticipantNameUpdatedPayload>(
            sp,
            options => new CommunicationSubscription(
                options.ParticipantNameUpdatedQueue,
                RoutingKeys.ParticipantNameUpdated,
                EventTypes.ParticipantNameUpdated)));

        return services;
    }

    private static CommunicationQueueConsumer<TPayload> CreateConsumer<TPayload>(
        IServiceProvider serviceProvider,
        Func<CommunicationQueueOptions, CommunicationSubscription> subscription)
    {
        var queues = serviceProvider.GetRequiredService<IOptions<CommunicationQueueOptions>>().Value;

        return new CommunicationQueueConsumer<TPayload>(
            serviceProvider.GetRequiredService<RabbitMqConnection>(),
            serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>(),
            serviceProvider.GetRequiredService<IOptions<CommunicationQueueOptions>>(),
            subscription(queues),
            serviceProvider.GetRequiredService<ICommunicationEventHandler<TPayload>>(),
            serviceProvider.GetRequiredService<ILogger<CommunicationQueueConsumer<TPayload>>>());
    }
}
