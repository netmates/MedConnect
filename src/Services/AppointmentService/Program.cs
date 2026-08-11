using AppointmentService.API.Middleware;
using AppointmentService.API.OpenApi;
using AppointmentService.Application.Extensions;
using AppointmentService.Infrastructure.Extensions;
using AppointmentService.Infrastructure.Persistence;
using AppointmentService.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog вместо стандартного провайдера логирования (конфиг из appsettings + enrichers)
builder.Host.AddAppointmentSerilog();

builder.Services.AddControllers();

// JWT через Keycloak (Authority, Audience, MapInboundClaims=false, роли из realm_access)
builder.Services.AddKeycloakJwtAuthentication(builder.Configuration);

// OpenAPI: Bearer (ручной JWT) + OAuth2 Password (логин/пароль → Keycloak)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddDocumentTransformer<KeycloakSecuritySchemeTransformer>();
});

// Application
builder.Services.AddApplicationServices();

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Health checks: /health/live (процесс), /health/ready (Postgres + Keycloak)
builder.Services.AddAppointmentHealthChecks(builder.Configuration);

// ProblemDetails + маппинг необработанных исключений → HTTP-статусы (404/403/400/500)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

try
{
    // При старте применяем EF-миграции (идемпотентно: если схема актуальна — ничего не делает)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppointmentDbContext>();
        await db.Database.MigrateAsync();
    }

    // Преднастроенные данные: справочник специализаций (если пуст); демо-врачи/пациенты — только Development
    await DataSeeder.SeedAsync(app.Services);

    // Включает перехват исключений в pipeline (использует GlobalExceptionHandler)
    app.UseExceptionHandler();

    // Сквозной id запроса (X-Correlation-ID) в LogContext — связывает HTTP и бизнес-логи
    app.UseCorrelationId();

    // Структурные логи HTTP: метод, путь, статус, длительность
    app.UseSerilogRequestLogging();

    // Эндпоинты /health/live (процесс) и /health/ready (Postgres + Keycloak), ответ JSON
    app.MapAppointmentHealthChecks();

    // OpenAPI-документ и UI Scalar
    if (app.Environment.IsDevelopment())
    {
        app.MapCommunicationScalar();
    }

    app.UseAuthentication();

    // UserId (JWT sub) в LogContext, для бизнес-логов
    app.UseUserIdLogContext();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
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
