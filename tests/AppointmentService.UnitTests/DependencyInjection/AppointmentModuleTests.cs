using AppointmentService.Application.DTOs.Doctor;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Caching;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Application.Services;
using AppointmentService.Common.DependencyInjection;
using AppointmentService.Domain.Entities;
using AppointmentService.Infrastructure.Persistence;
using AppointmentService.Infrastructure.Repositories;
using FluentValidation;
using MedConnect.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AppointmentService.UnitTests.DependencyInjection;

public class AppointmentModuleTests
{
    [Fact]
    public void Scan_ResolvesDoctorServiceAndRepository()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        AppointmentModule.AddScannedServices(services);

        services.AddDbContext<AppointmentDbContext>(options =>
            options.UseNpgsql("Host=127.0.0.1;Database=medconnect_di;Username=postgres;Password=postgres"));
        services.AddSingleton(Mock.Of<IDoctorListCache>());
        services.AddSingleton(Mock.Of<IDoctorAvailableSlotCache>());
        services.AddSingleton(Mock.Of<IUnitOfWork>());
        services.AddSingleton(Mock.Of<IKeycloakAdminService>());
        services.AddSingleton(Mock.Of<IValidator<CreateDoctorDto>>());
        services.AddSingleton(Mock.Of<IValidator<UpdateDoctorDto>>());
        services.AddSingleton(Mock.Of<IValidator<ResetPasswordDto>>());
        services.AddSingleton(Mock.Of<IIntegrationEventPublisher>());
        services.AddSingleton(Mock.Of<ICorrelationIdAccessor>());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var doctors = scope.ServiceProvider.GetRequiredService<IDoctorApplicationService>();
        var repository = scope.ServiceProvider.GetRequiredService<IDoctorRepository>();

        Assert.IsType<DoctorApplicationService>(doctors);
        Assert.IsType<DoctorRepository>(repository);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IRepository<Doctor>));
    }
}
