using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Infrastructure.Caching;
using AppointmentService.Infrastructure.Http;
using AppointmentService.Infrastructure.Keycloak;
using AppointmentService.Infrastructure.Messaging;
using AppointmentService.Infrastructure.Persistence;
using AppointmentService.Infrastructure.Repositories;
using MedConnect.Shared.Auth;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    private const string PostgresConnectionStringName = "Postgres";
    public const string RedisConnectionStringName = "Redis";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var postgres = configuration.GetConnectionString(PostgresConnectionStringName)
            ?? throw new InvalidOperationException($"ConnectionStrings:{PostgresConnectionStringName} не задан.");

        // EF Core + PostgreSQL
        services.AddDbContext<AppointmentDbContext>(options => options.UseNpgsql(postgres));

        var redis = configuration.GetConnectionString(RedisConnectionStringName)
            ?? throw new InvalidOperationException($"ConnectionStrings:{RedisConnectionStringName} не задан.");

        // Redis
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redis;
            options.InstanceName = "AppointmentService:";
        });
        // Кеш для списка специализаций
        services.AddSingleton<ISpecializationListCache, SpecializationListCache>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IScheduleSlotRepository, ScheduleSlotRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<ISpecializationRepository, SpecializationRepository>();

        // CorrelationId (X-Correlation-ID) из текущего HTTP-запроса для публикации событий
        services.AddScoped<ICorrelationIdAccessor, HttpCorrelationIdAccessor>();

        // Общий кеш access-токена Keycloak Admin API (сервис — Transient через HttpClient)
        services.AddSingleton<IKeycloakTokenCache, KeycloakTokenCache>();

        // Keycloak Admin API
        services.AddHttpClient<IKeycloakAdminService, KeycloakAdminService>(client =>
        {
            client.BaseAddress = new Uri(
                KeycloakConfiguration.GetRequired(configuration, nameof(KeycloakOptions.AdminApiUrl)));
        });

        // RabbitMQ: publisher AppointmentCreated, AppointmentCancelled, ParticipantNameUpdated → medconnect.events
        services.AddRabbitMqPublisher(configuration);

        return services;
    }
}
