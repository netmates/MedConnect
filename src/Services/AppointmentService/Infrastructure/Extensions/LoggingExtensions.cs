using Serilog;

namespace AppointmentService.Infrastructure.Extensions;

public static class LoggingExtensions
{
    public static void AddAppointmentSerilog(this IHostBuilder host)
    {
        host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", "AppointmentService")
                .Enrich.WithProperty("EnvironmentName", context.HostingEnvironment.EnvironmentName);

            if (context.HostingEnvironment.IsDevelopment())
            {
                var seqUrl = context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341";
                configuration.WriteTo.Seq(seqUrl);
            }
        });
    }
}
