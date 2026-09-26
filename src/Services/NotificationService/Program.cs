using MedConnect.Messaging;
using NotificationService.Common.Health;
using NotificationService.Common.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddNotificationSerilog();

// Соединение с RabbitMQ при старте. Очереди объявят consumers на следующем шаге.
builder.Services.AddRabbitMq(builder.Configuration, "NotificationService");
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
