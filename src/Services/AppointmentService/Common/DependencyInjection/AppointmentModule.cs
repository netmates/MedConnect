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
using Scrutor;

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
        services.Scan(scan => scan
            .FromAssemblyOf<DoctorApplicationService>()
            .AddClasses(classes => classes.Where(IsApplicationService))
            .AsMatchingInterface()
            .WithScopedLifetime());

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

        services.AddDbContext<AppointmentDbContext>(options => options.UseNpgsql(postgres));

        var redis = configuration.GetConnectionString(RedisConnectionStringName)
            ?? throw new InvalidOperationException($"ConnectionStrings:{RedisConnectionStringName} не задан.");

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redis;
            options.InstanceName = "AppointmentService:";
        });
        services.AddSingleton<ISpecializationListCache, SpecializationListCache>();
        services.AddSingleton<IDoctorListCache, DoctorListCache>();
        services.AddSingleton<IDoctorAvailableSlotCache, DoctorAvailableSlotCache>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICorrelationIdAccessor, HttpCorrelationIdAccessor>();
        services.AddSingleton<IKeycloakTokenCache, KeycloakTokenCache>();

        services.AddHttpClient<IKeycloakAdminService, KeycloakAdminService>(client =>
        {
            client.BaseAddress = new Uri(
                KeycloakConfiguration.GetRequired(configuration, nameof(KeycloakOptions.AdminApiUrl)));
        });

        services.AddRabbitMqPublisher(configuration);
    }

    private static bool IsApplicationService(Type type) =>
        type is { IsAbstract: false, IsGenericTypeDefinition: false }
        && (type.Name.EndsWith("ApplicationService", StringComparison.Ordinal)
            || type == typeof(ActiveAppointmentCancellation));
}
