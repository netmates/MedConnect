using MedConnect.Messaging;

namespace AppointmentService.Infrastructure.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMqPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddRabbitMq(configuration, "AppointmentService");
    }
}
