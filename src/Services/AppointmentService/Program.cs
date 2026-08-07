using AppointmentService.API.Middleware;
using AppointmentService.API.OpenApi;
using AppointmentService.Application.Extensions;
using AppointmentService.Infrastructure.Extensions;
using AppointmentService.Infrastructure.Persistence;
using AppointmentService.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

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

// Liveness: процесс
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = HealthChecksExtensions.WriteHealthJson
}).AllowAnonymous();

// Readiness: Postgres + Keycloak
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthChecksExtensions.WriteHealthJson
}).AllowAnonymous();

// OpenAPI-документ и UI Scalar
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        var clientId = app.Configuration["Keycloak:Audience"]
            ?? throw new InvalidOperationException("Keycloak:Audience не задан.");

        options
            .AddPreferredSecuritySchemes(KeycloakSecuritySchemeTransformer.SchemeId)
            .AddPasswordFlow(KeycloakSecuritySchemeTransformer.SchemeId, flow =>
            {
                flow.ClientId = clientId;
                flow.Username = "admin1";
                flow.SelectedScopes = ["openid"];

                flow.WithCredentialsLocation(CredentialsLocation.Body);
            });
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
