using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Infrastructure.Persistence;
using AppointmentService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core + PostgreSQL
        services.AddDbContext<AppointmentDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        // Unit of Work        
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IScheduleSlotRepository, ScheduleSlotRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<ISpecializationRepository, SpecializationRepository>();

        return services;
    }
}
