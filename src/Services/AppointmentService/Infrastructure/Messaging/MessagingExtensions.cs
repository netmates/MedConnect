using AppointmentService.Application.Interfaces.Services;

namespace AppointmentService.Infrastructure.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMqPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IIntegrationEventPublisher, RabbitMqPublisher>();
        services.AddHostedService<RabbitMqConnectionHostedService>();

        return services;
    }

    private sealed class RabbitMqConnectionHostedService(RabbitMqConnection connection) : IHostedService
    {
        public async Task StartAsync(CancellationToken ct) => await connection.GetConnectionAsync(ct);
        public async Task StopAsync(CancellationToken ct) => await connection.DisposeAsync();
    }
}
