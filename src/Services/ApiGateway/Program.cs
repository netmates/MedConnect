using ApiGateway.Auth;
using MedConnect.Shared.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog вместо стандартного провайдера логирования (конфиг из appsettings + enrichers)
builder.Host.AddMedConnectSerilog("ApiGateway");

// YARP: маршруты и кластеры из секции ReverseProxy
// /api/chats -> CommunicationService, остальные /api -> AppointmentService
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

try
{
    // /health/live: процесс жив. Путь не под /api, токен не требуется
    app.MapGet("/health/live", () => Results.Ok());

    // /api без токена -> 401. Без проверки подписи JWT.
    // Исключения: POST /api/patients/sign-up и SignalR /api/chats/hub с access_token в query
    app.UseMiddleware<TokenPresenceMiddleware>();

    // Проксирование по маршрутам YARP
    app.MapReverseProxy();

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
