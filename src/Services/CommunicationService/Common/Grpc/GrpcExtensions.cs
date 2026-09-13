using MedConnect.Shared.Grpc;
using Microsoft.Extensions.Options;

namespace CommunicationService.Common.Grpc;

public static class GrpcExtensions
{
    public static IServiceCollection AddAppointmentGrpcClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AppointmentAccessOptions>(configuration.GetSection(AppointmentAccessOptions.SectionName));

        services.AddGrpcClient<AppointmentAccess.AppointmentAccessClient>((sp, options) =>
            {
                var address = sp.GetRequiredService<IOptions<AppointmentAccessOptions>>().Value.Address;
                if (string.IsNullOrWhiteSpace(address))
                    throw new InvalidOperationException($"{AppointmentAccessOptions.SectionName}:Address не задан.");
                options.Address = new Uri(address);
            });

        services.AddScoped<AppointmentAccessClient>();
        return services;
    }
}
