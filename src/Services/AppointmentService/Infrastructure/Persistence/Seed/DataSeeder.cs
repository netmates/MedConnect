using AppointmentService.Application.DTOs.Doctor;
using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    private static readonly string[] SpecializationNames =
    [
        "Терапевт",
        "Кардиолог",
        "Невролог",
        "Педиатр",
        "Дерматолог"
    ];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppointmentDbContext>();
        var env = sp.GetRequiredService<IHostEnvironment>();
        var config = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeder");

        var seedSpecializations = config.GetValue("Seed:Specializations", true);
        var seedDemoUsers = config.GetValue("Seed:DemoUsers", false);

        if (seedSpecializations)
            await SeedSpecializationsIfEmptyAsync(db, logger, ct);
        
        if (env.IsDevelopment() && seedDemoUsers)
        {
            var keycloak = sp.GetRequiredService<IKeycloakAdminService>();

            var doctorService = sp.GetRequiredService<IDoctorApplicationService>();
            await SeedDemoDoctorsAsync(db, doctorService, keycloak, logger, ct);

            var patientService = sp.GetRequiredService<IPatientApplicationService>();            
            await SeedDemoPatientsAsync(db, patientService, keycloak, logger, ct);
        }
    }

    private static async Task SeedSpecializationsIfEmptyAsync(
        AppointmentDbContext db,
        ILogger logger,
        CancellationToken ct)
    {
        if (await db.Specializations.AnyAsync(ct))
        {
            logger.LogInformation("Specializations already present — skip seed");
            return;
        }

        foreach (var name in SpecializationNames)
            await db.Specializations.AddAsync(Specialization.Create(name), ct);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} specializations", SpecializationNames.Length);
    }

    private static async Task SeedDemoDoctorsAsync(
        AppointmentDbContext db,
        IDoctorApplicationService doctorService,
        IKeycloakAdminService keycloak,
        ILogger logger,
        CancellationToken ct)
    {
        if (await db.Doctors.AnyAsync(ct))
        {
            logger.LogInformation("Doctors already present — skip demo doctors seed");
            return;
        }

        var specs = await db.Specializations
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Name, s => s.Id, ct);

        var doctors = new[]
        {
            new CreateDoctorDto
            {
                LastName = "Иванов",
                FirstName = "Иван",
                MiddleName = "Иванович",
                Email = "doctor1@medconnect.local",
                TemporaryPassword = "Doctor1Pass!",
                Description = "Терапевт, стаж 10 лет",
                ExperienceYears = 10,
                SpecializationIds = [specs["Терапевт"]]
            },
            new CreateDoctorDto
            {
                LastName = "Петрова",
                FirstName = "Анна",
                MiddleName = "Сергеевна",
                Email = "doctor2@medconnect.local",
                TemporaryPassword = "Doctor2Pass!",
                Description = "Кардиолог",
                ExperienceYears = 8,
                SpecializationIds = [specs["Кардиолог"], specs["Терапевт"]]
            }
        };

        foreach (var dto in doctors)
        {
            var created = await doctorService.CreateAsync(dto, ct);
            await keycloak.ResetPasswordAsync(created.KeycloakId, dto.TemporaryPassword, ct);
            logger.LogInformation("Seeded doctor {Email}", dto.Email);
        }
    }

    private static async Task SeedDemoPatientsAsync(
        AppointmentDbContext db,
        IPatientApplicationService patientService,
        IKeycloakAdminService keycloak,
        ILogger logger,
        CancellationToken ct)
    {
        if (await db.Patients.AnyAsync(ct))
        {
            logger.LogInformation("Patients already present — skip");
            return;
        }

        var patients = new[]
        {
            new
            {
                Email = "patient1@medconnect.local",
                Password = "Patient1Pass!",
                Dto = new RegisterPatientDto
                {
                    LastName = "Сидоров",
                    FirstName = "Пётр",
                    MiddleName = "Алексеевич",
                    Phone = "+79001112233",
                    DateOfBirth = new DateTime(1990, 5, 15, 0, 0, 0, DateTimeKind.Utc)
                }
            },
            new
            {
                Email = "patient2@medconnect.local",
                Password = "Patient2Pass!",
                Dto = new RegisterPatientDto
                {
                    LastName = "Козлова",
                    FirstName = "Мария",
                    MiddleName = null,
                    Phone = "+79005556677",
                    DateOfBirth = new DateTime(1985, 3, 20, 0, 0, 0, DateTimeKind.Utc)
                }
            }
        };

        foreach (var p in patients)
        {
            var keycloakId = await keycloak.CreateUserAsync(
                email: p.Email,
                temporaryPassword: p.Password,
                role: "patient",
                firstName: p.Dto.FirstName,
                lastName: p.Dto.LastName,
                ct: ct);

            await keycloak.ResetPasswordAsync(keycloakId, p.Password, ct);
            await patientService.RegisterOrGetAsync(keycloakId, p.Dto, ct);
            logger.LogInformation("Seeded patient {Email}", p.Email);
        }
    }
}
