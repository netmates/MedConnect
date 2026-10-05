using MedConnect.Messaging;

namespace CommunicationService.Common.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMqConsumer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRabbitMq(configuration, "CommunicationService");

        services
            .AddOptions<CommunicationQueueOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHostedService<AppointmentCreatedConsumer>();
        services.AddHostedService<ParticipantNameUpdatedConsumer>();

        return services;
    }
}
