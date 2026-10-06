using NotificationService.Common.Health;
using NotificationService.Common.Logging;
using NotificationService.Common.Messaging;
using NotificationService.Features.Notifications;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog вместо стандартного провайдера логирования (конфиг из appsettings + enrichers)
builder.Host.AddNotificationSerilog();

// Каналы уведомлений (Fake / Email / Sms) и обработчики AppointmentCreated, AppointmentCancelled, MessageCreated
new NotificationModule().Register(builder.Services, builder.Configuration);

// RabbitMQ: consumer трех очередей уведомлений
builder.Services.AddNotificationConsumers(builder.Configuration);

// Health checks: self (live) + RabbitMQ (ready)
builder.Services.AddNotificationHealthChecks();

var app = builder.Build();

try
{
    // Эндпоинты /health/live (процесс) и /health/ready (RabbitMQ)
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
