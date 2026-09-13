using CommunicationService.Common.Auth;
using CommunicationService.Common.Grpc;
using CommunicationService.Common.Health;
using CommunicationService.Common.Logging;
using CommunicationService.Common.Messaging;
using CommunicationService.Common.Middleware;
using CommunicationService.Common.OpenApi;
using CommunicationService.Common.Persistence;
using CommunicationService.Common.SignalR;
using CommunicationService.Features;
using CommunicationService.Features.Chats;
using CommunicationService.Features.Messages;
using FluentValidation;
using MongoDB.Driver;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog вместо стандартного провайдера логирования (конфиг из appsettings + enrichers)
builder.Host.AddCommunicationSerilog();

builder.Services.AddAuthorization();

// JWT через Keycloak (Authority, Audience, MapInboundClaims=false, роли из realm_access → claim role)
builder.Services.AddKeycloakJwtAuthentication(builder.Configuration);

// OpenAPI: Bearer (ручной JWT) + OAuth2 Password (логин/пароль → Keycloak)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddDocumentTransformer<KeycloakSecuritySchemeTransformer>();
});

// FluentValidation: IValidator<> из сборки
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// CreateChat: создать чат по appointment (идемпотентно)
builder.Services.AddScoped<CreateChatHandler>();
// GetChatHistory: история сообщений для участника чата
builder.Services.AddScoped<GetChatHistoryHandler>();
// SendMessage: отправить сообщение в чат
builder.Services.AddScoped<SendMessageHandler>();
// Общий Mongo ensure-chat (HTTP CreateChat + AppointmentCreated consumer)
builder.Services.AddScoped<EnsureChatService>();
// Sync ФИО участников чата из RabbitMQ (ParticipantNameUpdated)
builder.Services.AddScoped<UpdateChatParticipantNamesService>();

// MongoDB: IMongoClient + IMongoDatabase из ConnectionStrings:Mongo и Mongo:Database
builder.Services.AddMongo(builder.Configuration);

// RabbitMQ: AppointmentCreated → EnsureChat; ParticipantNameUpdated → sync ФИО в чатах
builder.Services.AddRabbitMqConsumer(builder.Configuration);

// gRPC-клиент к AppointmentService: проверка записи перед открытием/созданием чата (AppointmentGrpc:Address)
builder.Services.AddAppointmentGrpcClient(builder.Configuration);

// SignalR
builder.Services.AddChatSignalR();

// Health checks: self (live) + MongoDB (ready)
builder.Services.AddCommunicationHealthChecks();

// ProblemDetails + маппинг необработанных исключений → HTTP-статусы (404/403/400/500)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

try
{
    // Индексы Mongo при старте: unique AppointmentId (chats), (ChatId, CreatedAt) у messages
    using (var scope = app.Services.CreateScope())
    {
        var mongo = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        await MongoIndexInitializer.EnsureIndexesAsync(mongo);
    }

    // Включает перехват исключений в pipeline (использует GlobalExceptionHandler)
    app.UseExceptionHandler();

    // Сквозной id запроса (X-Correlation-ID) в LogContext — связывает HTTP и бизнес-логи
    app.UseCorrelationId();

    // Структурные логи HTTP: метод, путь, статус, длительность
    app.UseSerilogRequestLogging();

    // Эндпоинты /health/live (процесс) и /health/ready (MongoDB)
    app.MapCommunicationHealthChecks();

    // OpenAPI-документ и UI Scalar
    if (app.Environment.IsDevelopment())
    {
        app.MapCommunicationScalar();
    }

    app.UseAuthentication();

    // UserId (JWT sub) в LogContext, для бизнес-логов
    app.UseUserIdLogContext();

    app.UseAuthorization();

    // API чатов: create / history / send (роли patient, doctor)
    app.MapFeatureEndpoints();

    // SignalR: real-time чат
    app.MapChatHub();

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
