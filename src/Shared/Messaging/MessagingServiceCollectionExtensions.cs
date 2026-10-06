using RabbitMQ.Client.Exceptions;

namespace MedConnect.Shared.Messaging;

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

    private sealed class RabbitMqConnectionHostedService(
        RabbitMqConnection connection,
        ILogger<RabbitMqConnectionHostedService> logger) : IHostedService
    {
        private const int MaxAttempts = 15;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

        public async Task StartAsync(CancellationToken ct)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await connection.GetConnectionAsync(ct);
                    return;
                }
                catch (BrokerUnreachableException ex) when (attempt < MaxAttempts)
                {
                    logger.LogWarning(
                        "RabbitMQ is not ready (attempt {Attempt}/{MaxAttempts}): {Message}",
                        attempt,
                        MaxAttempts,
                        ex.Message);

                    await Task.Delay(RetryDelay, ct);
                }
            }
        }

        public async Task StopAsync(CancellationToken ct) => await connection.DisposeAsync();
    }
}
