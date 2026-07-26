using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Application.Services;
using AppointmentService.Application.Validators;
using FluentValidation;

namespace AppointmentService.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Services
        services.AddScoped<IDoctorApplicationService, DoctorApplicationService>();
        services.AddScoped<IAppointmentApplicationService, AppointmentApplicationService>();
        services.AddScoped<IPatientApplicationService, PatientApplicationService>();
        services.AddScoped<ISpecializationApplicationService, SpecializationApplicationService>();
        services.AddScoped<IScheduleSlotApplicationService, ScheduleSlotApplicationService>();        
        services.AddScoped<IAdminPatientApplicationService, AdminPatientApplicationService>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<CreateDoctorValidator>();

        return services;
    }
}
