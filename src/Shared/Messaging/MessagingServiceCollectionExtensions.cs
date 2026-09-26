using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MedConnect.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration,
        string eventSource)
    {
        if (string.IsNullOrWhiteSpace(eventSource))
            throw new ArgumentException("Event source is required.", nameof(eventSource));

        services
            .AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IIntegrationEventPublisher>(sp =>
            ActivatorUtilities.CreateInstance<RabbitMqPublisher>(sp, eventSource));
        services.AddHostedService<RabbitMqConnectionHostedService>();

        return services;
    }

    private sealed class RabbitMqConnectionHostedService(RabbitMqConnection connection) : IHostedService
    {
        public async Task StartAsync(CancellationToken ct) => await connection.GetConnectionAsync(ct);

        public async Task StopAsync(CancellationToken ct) => await connection.DisposeAsync();
    }
}
