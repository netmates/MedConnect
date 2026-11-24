using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Caching;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Application.Services;
using AppointmentService.Application.Validators;
using AppointmentService.Infrastructure;
using AppointmentService.Infrastructure.Caching;
using AppointmentService.Infrastructure.Http;
using AppointmentService.Infrastructure.Keycloak;
using AppointmentService.Infrastructure.Messaging;
using AppointmentService.Infrastructure.Persistence;
using AppointmentService.Infrastructure.Repositories;
using FluentValidation;
using MedConnect.Shared.Auth;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Common.DependencyInjection;

public sealed class AppointmentModule : IServiceModule
{
    private const string PostgresConnectionStringName = "Postgres";
    public const string RedisConnectionStringName = "Redis";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        AddScannedServices(services);
        services.AddValidatorsFromAssemblyContaining<CreateDoctorValidator>();
        AddConfiguredInfrastructure(services, configuration);
    }

    public static void AddScannedServices(IServiceCollection services)
    {
        // Имя заканчивается на ApplicationService. ActiveAppointmentCancellation указан отдельно: у него другое имя.
        // Интерфейс — I и имя класса, один объект на запрос
        services.Scan(scan => scan
            .FromAssemblyOf<DoctorApplicationService>()
            .AddClasses(classes => classes.Where(IsApplicationService))
            .AsMatchingInterface()
            .WithScopedLifetime());

        // Пространство имен Infrastructure.Repositories, кроме открытого Repository<T>. Интерфейс — I и имя класса, один объект на запрос
        services.Scan(scan => scan
            .FromAssemblyOf<DoctorRepository>()
            .AddClasses(classes => classes
                .InNamespaceOf<DoctorRepository>()
                .Where(type => !type.IsGenericTypeDefinition))
            .AsMatchingInterface()
            .WithScopedLifetime());
    }

    private static void AddConfiguredInfrastructure(IServiceCollection services, IConfiguration configuration)
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
        // Кеш для списка врачей
        services.AddSingleton<IDoctorListCache, DoctorListCache>();
        // Кеш для свободных слотов врача
        services.AddSingleton<IDoctorAvailableSlotCache, DoctorAvailableSlotCache>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
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
    }

    private static bool IsApplicationService(Type type) =>
        type is { IsAbstract: false, IsGenericTypeDefinition: false }
        && (type.Name.EndsWith("ApplicationService", StringComparison.Ordinal)
            || type == typeof(ActiveAppointmentCancellation));
}
