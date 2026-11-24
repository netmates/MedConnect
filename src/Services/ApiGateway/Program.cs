using ApiGateway.Auth;
using MedConnect.Shared.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog вместо стандартного провайдера логирования (конфиг из appsettings + enrichers)
builder.Host.AddMedConnectSerilog("ApiGateway");

// JWT Keycloak: подпись, издатель, аудитория и срок; роли проверяют сервисы
builder.Services.AddGatewayJwtAuthentication(builder.Configuration);

// YARP: маршруты и кластеры из секции ReverseProxy
// /api/chats -> CommunicationService, остальные /api -> AppointmentService
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

try
{
    // /health/live: процесс жив. Путь не под /api, токен не требуется
    app.MapGet("/health/live", () => Results.Ok());

    // /api с неверным или просроченным токеном -> 401.
    // Исключение: POST /api/patients/sign-up проходит без токена
    app.UseMiddleware<TokenValidationMiddleware>();

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
