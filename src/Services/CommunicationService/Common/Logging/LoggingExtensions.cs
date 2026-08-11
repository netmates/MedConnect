using Serilog;

namespace CommunicationService.Common.Logging;

public static class LoggingExtensions
{
    public static void AddCommunicationSerilog(this IHostBuilder host)
    {
        host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", "CommunicationService")
                .Enrich.WithProperty("EnvironmentName", context.HostingEnvironment.EnvironmentName);

            if (context.HostingEnvironment.IsDevelopment())
                configuration.WriteTo.Seq("http://localhost:5341");
        });
    }
}
