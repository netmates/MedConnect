using NotificationService.Common.Health;
using NotificationService.Common.Logging;
using NotificationService.Common.Messaging;
using NotificationService.Features.Notifications;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddNotificationSerilog();

new NotificationModule().Register(builder.Services, builder.Configuration);
builder.Services.AddNotificationConsumers(builder.Configuration);
builder.Services.AddNotificationHealthChecks();

var app = builder.Build();

try
{
    app.MapNotificationHealthChecks();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
