namespace CommunicationService.Common.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMqConsumer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<RabbitMqConnection>();
        services.AddHostedService<AppointmentCreatedConsumer>();

        return services;
    }
}
