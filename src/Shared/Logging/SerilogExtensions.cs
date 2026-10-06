using Serilog;

namespace MedConnect.Shared.Logging;

public static class SerilogExtensions
{
    public static void AddMedConnectSerilog(this IHostBuilder host, string serviceName)
    {
        host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithProperty("EnvironmentName", context.HostingEnvironment.EnvironmentName);

            if (context.HostingEnvironment.IsDevelopment())
            {
                var seqUrl = context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341";
                configuration.WriteTo.Seq(seqUrl);
            }
        });
    }
}
