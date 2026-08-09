using AppointmentService.Application.DTOs.Doctor;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Application.Services;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;

namespace AppointmentService.UnitTests.Application;

public class DoctorApplicationServiceTests
{
    private readonly Mock<IDoctorRepository> _doctors = new();
    private readonly Mock<ISpecializationRepository> _specializations = new();
    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly Mock<IScheduleSlotRepository> _slots = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IKeycloakAdminService> _keycloak = new();
    private readonly Mock<IValidator<CreateDoctorDto>> _createValidator = new();
    private readonly Mock<IValidator<UpdateDoctorDto>> _updateValidator = new();
    private readonly Mock<IValidator<ResetPasswordDto>> _resetPasswordValidator = new();

    private readonly DoctorApplicationService _sut;

    private static readonly DateTime FutureStart =
        new(2030, 6, 15, 10, 0, 0, DateTimeKind.Utc);

    public DoctorApplicationServiceTests()
    {
        _createValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateDoctorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _updateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateDoctorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _resetPasswordValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ResetPasswordDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new DoctorApplicationService(
            _doctors.Object,
            _specializations.Object,
            _appointments.Object,
            _slots.Object,
            _uow.Object,
            _keycloak.Object,
            _createValidator.Object,
            _updateValidator.Object,
            _resetPasswordValidator.Object,
            NullLogger<DoctorApplicationService>.Instance);
    }

    private static Doctor CreateDoctor(string keycloakId = "doctor-kc")
        => Doctor.Create(keycloakId, "Петров", "Петр", "Петрович", "Терапевт", 10);

    private static Patient CreatePatient(string keycloakId = "patient-kc")
        => Patient.Create(keycloakId, "Иванов", "Иван", null, "+79001234567", new DateTime(1990, 1, 1));

    private static Specialization CreateSpecialization(string name = "Терапия")
        => Specialization.Create(name);

    private static ScheduleSlot CreateFutureSlot(Guid doctorId)
        => ScheduleSlot.Create(doctorId, FutureStart, FutureStart.AddMinutes(30));

    private static void AttachSpecialization(Doctor doctor, Specialization specialization)
    {
        var link = DoctorSpecialization.Create(doctor.Id, specialization.Id);
        typeof(DoctorSpecialization)
            .GetProperty(nameof(DoctorSpecialization.Specialization), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(link, specialization);
        doctor.DoctorSpecializations.Add(link);
    }

    private static CreateDoctorDto ValidCreateDto(params Guid[] specializationIds)
        => new()
        {
            LastName = "Петров",
            FirstName = "Петр",
            MiddleName = "Петрович",
            Email = "doctor@medconnect.local",
            TemporaryPassword = "TempPass1!",
            Description = "Терапевт",
            ExperienceYears = 10,
            SpecializationIds = specializationIds.ToList()
        };

    private static UpdateDoctorDto ValidUpdateDto(params Guid[] specializationIds)
        => new()
        {
            LastName = "Сидоров",
            FirstName = "Сидор",
            MiddleName = null,
            Description = "Хирург",
            ExperienceYears = 15,
            SpecializationIds = specializationIds.ToList()
        };

    // GetAll

    [Fact]
    public async Task GetAllAsync_WithoutSpecializationId_UsesGetActive()
    {
        // Arrange
        var doctor = CreateDoctor();
        var spec = CreateSpecialization();
        AttachSpecialization(doctor, spec);
        _doctors.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([doctor]);

        // Act
        var result = await _sut.GetAllAsync(null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(doctor.Id, result[0].Id);
        _doctors.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
        _doctors.Verify(
            r => r.GetBySpecializationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WithSpecializationId_UsesGetBySpecialization()
    {
        // Arrange
        var specId = Guid.NewGuid();
        var doctor = CreateDoctor();
        _doctors.Setup(r => r.GetBySpecializationAsync(specId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([doctor]);

        // Act
        var result = await _sut.GetAllAsync(specId, CancellationToken.None);

        // Assert
        Assert.Single(result);
        _doctors.Verify(r => r.GetBySpecializationAsync(specId, It.IsAny<CancellationToken>()), Times.Once);
        _doctors.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // GetById

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ThrowsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _doctors.Setup(r => r.GetWithSpecializationsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetByIdAsync(id, CancellationToken.None));

        // Assert
        Assert.Equal($"Врач {id} не найден.", ex.Message);
    }

    [Fact]
    public async Task GetByIdAsync_WhenInactive_ThrowsNotFound()
    {
        // Arrange
        var doctor = CreateDoctor();
        doctor.Deactivate();
        _doctors.Setup(r => r.GetWithSpecializationsAsync(doctor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetByIdAsync(doctor.Id, CancellationToken.None));

        // Assert
        Assert.Equal($"Врач {doctor.Id} не найден.", ex.Message);
    }

    [Fact]
    public async Task GetByIdAsync_WhenActive_ReturnsDtoWithSpecializations()
    {
        // Arrange
        var doctor = CreateDoctor();
        var spec = CreateSpecialization("Кардиология");
        AttachSpecialization(doctor, spec);
        _doctors.Setup(r => r.GetWithSpecializationsAsync(doctor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);

        // Act
        var result = await _sut.GetByIdAsync(doctor.Id, CancellationToken.None);

        // Assert
        Assert.Equal(doctor.Id, result.Id);
        Assert.True(result.IsActive);
        Assert.Contains("Кардиология", result.Specializations);
    }

    // GetAllIncludingInactive

    [Fact]
    public async Task GetAllIncludingInactiveAsync_ReturnsMappedList()
    {
        // Arrange
        var doctor = CreateDoctor();
        doctor.Deactivate();
        _doctors.Setup(r => r.GetAllIncludingInactiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([doctor]);

        // Act
        var result = await _sut.GetAllIncludingInactiveAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.False(result[0].IsActive);
        Assert.Equal(doctor.Id, result[0].Id);
    }

    // Create

    [Fact]
    public async Task CreateAsync_WhenValidationFails_ThrowsValidationException()
    {
        // Arrange
        var failures = new[] { new ValidationFailure("Email", "Email обязателен.") };
        _createValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateDoctorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateAsync(ValidCreateDto(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WhenSpecializationNotFound_RollsBackAndDeletesKeycloakUser()
    {
        // Arrange
        var missingSpecId = Guid.NewGuid();
        var dto = ValidCreateDto(missingSpecId);
        const string keycloakId = "kc-new-doctor";

        _keycloak.Setup(k => k.CreateUserAsync(
                dto.Email, dto.TemporaryPassword, "doctor",
                dto.FirstName, dto.LastName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(keycloakId);
        _specializations.Setup(r => r.GetByIdAsync(missingSpecId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Specialization?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.CreateAsync(dto, CancellationToken.None));

        // Assert
        Assert.Equal($"Специализация {missingSpecId} не найдена.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _keycloak.Verify(k => k.DeleteUserAsync(keycloakId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_CreatesDoctorLinksSpecsAndReturnsDto()
    {
        // Arrange
        var spec = CreateSpecialization();
        var dto = ValidCreateDto(spec.Id);
        const string keycloakId = "kc-new-doctor";
        Doctor? added = null;

        _keycloak.Setup(k => k.CreateUserAsync(
                dto.Email, dto.TemporaryPassword, "doctor",
                dto.FirstName, dto.LastName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(keycloakId);
        _specializations.Setup(r => r.GetByIdAsync(spec.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(spec);
        _doctors
            .Setup(r => r.AddAsync(It.IsAny<Doctor>(), It.IsAny<CancellationToken>()))
            .Callback<Doctor, CancellationToken>((d, _) => added = d)
            .Returns(Task.CompletedTask);
        _doctors
            .Setup(r => r.GetWithSpecializationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                AttachSpecialization(added!, spec);
                return added;
            });

        // Act
        var result = await _sut.CreateAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(added);
        Assert.Equal(keycloakId, result.KeycloakId);
        Assert.Equal(dto.LastName, result.LastName);
        Assert.Contains(spec.Name, result.Specializations);
        _doctors.Verify(
            r => r.AddDoctorSpecializationAsync(It.IsAny<DoctorSpecialization>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Update

    [Fact]
    public async Task UpdateAsync_WhenValidationFails_ThrowsValidationException()
    {
        // Arrange
        var failures = new[] { new ValidationFailure("LastName", "Фамилия обязательна.") };
        _updateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateDoctorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.UpdateAsync(Guid.NewGuid(), ValidUpdateDto(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WhenDoctorNotFound_ThrowsNotFoundAndRollsBack()
    {
        // Arrange
        var id = Guid.NewGuid();
        _doctors.Setup(r => r.GetWithSpecializationsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.UpdateAsync(id, ValidUpdateDto(Guid.NewGuid()), CancellationToken.None));

        // Assert
        Assert.Equal($"Врач {id} не найден.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenSpecializationNotFound_ThrowsNotFoundAndRollsBack()
    {
        // Arrange
        var doctor = CreateDoctor();
        var missingSpecId = Guid.NewGuid();
        _doctors.Setup(r => r.GetWithSpecializationsAsync(doctor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _specializations.Setup(r => r.GetByIdAsync(missingSpecId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Specialization?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.UpdateAsync(doctor.Id, ValidUpdateDto(missingSpecId), CancellationToken.None));

        // Assert
        Assert.Equal($"Специализация {missingSpecId} не найдена.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenSpecializationIdsEmpty_ThrowsBusinessRule()
    {
        // Arrange — валидатор замокан как valid, бизнес-правило в сервисе
        var doctor = CreateDoctor();
        var existing = CreateSpecialization();
        AttachSpecialization(doctor, existing);
        _doctors.Setup(r => r.GetWithSpecializationsAsync(doctor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);

        var dto = ValidUpdateDto();
        dto.SpecializationIds = [];

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.UpdateAsync(doctor.Id, dto, CancellationToken.None));

        // Assert
        Assert.Equal("Врач должен иметь хотя бы одну специализацию.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_SyncsSpecializationsAndReturnsDto()
    {
        // Arrange
        var doctor = CreateDoctor();
        var oldSpec = CreateSpecialization("Старая");
        var newSpec = CreateSpecialization("Новая");
        AttachSpecialization(doctor, oldSpec);

        var dto = ValidUpdateDto(newSpec.Id);

        _doctors.SetupSequence(r => r.GetWithSpecializationsAsync(doctor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor)
            .ReturnsAsync(() =>
            {
                doctor.DoctorSpecializations.Clear();
                AttachSpecialization(doctor, newSpec);
                return doctor;
            });
        _specializations.Setup(r => r.GetByIdAsync(newSpec.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newSpec);

        // Act
        var result = await _sut.UpdateAsync(doctor.Id, dto, CancellationToken.None);

        // Assert
        Assert.Equal("Сидоров", result.LastName);
        Assert.Contains("Новая", result.Specializations);
        _doctors.Verify(
            r => r.AddDoctorSpecializationAsync(
                It.Is<DoctorSpecialization>(ds => ds.SpecializationId == newSpec.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _doctors.Verify(
            r => r.RemoveDoctorSpecializationAsync(doctor.Id, oldSpec.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Deactivate

    [Fact]
    public async Task DeactivateAsync_WhenDoctorNotFound_ThrowsNotFoundAndRollsBack()
    {
        // Arrange
        var id = Guid.NewGuid();
        _doctors.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.DeactivateAsync(id, CancellationToken.None));

        // Assert
        Assert.Equal($"Врач {id} не найден.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _keycloak.Verify(
            k => k.DisableUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeactivateAsync_WhenNoActiveAppointments_DeactivatesAndDisablesInKeycloak()
    {
        // Arrange
        var doctor = CreateDoctor();
        _doctors.Setup(r => r.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _appointments.Setup(r => r.GetActiveFutureByDoctorIdAsync(
                doctor.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await _sut.DeactivateAsync(doctor.Id, CancellationToken.None);

        // Assert
        Assert.False(doctor.IsActive);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _keycloak.Verify(
            k => k.DisableUserAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_WhenHasActiveAppointments_CancelsThemAndFreesSlots()
    {
        // Arrange
        var doctor = CreateDoctor();
        var patient = CreatePatient();
        var slot = CreateFutureSlot(doctor.Id);
        slot.Book();
        var appointment = Appointment.Create(patient.Id, doctor.Id, slot.Id, "Осмотр");

        _doctors.Setup(r => r.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _appointments.Setup(r => r.GetActiveFutureByDoctorIdAsync(
                doctor.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([appointment]);
        _appointments.Setup(r => r.GetByIdWithLockAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);
        _slots.Setup(r => r.GetByIdWithLockAsync(appointment.SlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);

        // Act
        await _sut.DeactivateAsync(doctor.Id, CancellationToken.None);

        // Assert
        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Equal(SlotStatus.Available, slot.Status);
        Assert.False(doctor.IsActive);
        _keycloak.Verify(
            k => k.DisableUserAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Activate

    [Fact]
    public async Task ActivateAsync_WhenDoctorNotFound_ThrowsNotFoundAndRollsBack()
    {
        // Arrange
        var id = Guid.NewGuid();
        _doctors.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.ActivateAsync(id, CancellationToken.None));

        // Assert
        Assert.Equal($"Врач {id} не найден.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _keycloak.Verify(
            k => k.EnableUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ActivateAsync_WhenExists_ActivatesAndEnablesInKeycloak()
    {
        // Arrange
        var doctor = CreateDoctor();
        doctor.Deactivate();
        _doctors.Setup(r => r.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);

        // Act
        await _sut.ActivateAsync(doctor.Id, CancellationToken.None);

        // Assert
        Assert.True(doctor.IsActive);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _keycloak.Verify(
            k => k.EnableUserAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ResetPassword

    [Fact]
    public async Task ResetPasswordAsync_WhenValidationFails_ThrowsValidationException()
    {
        // Arrange
        var failures = new[] { new ValidationFailure("NewPassword", "Новый пароль обязателен.") };
        _resetPasswordValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ResetPasswordDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ResetPasswordAsync(Guid.NewGuid(), new ResetPasswordDto { NewPassword = "" }, CancellationToken.None));
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenDoctorNotFound_ThrowsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _doctors.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.ResetPasswordAsync(id, new ResetPasswordDto { NewPassword = "NewPass1!" }, CancellationToken.None));

        // Assert
        Assert.Equal($"Врач {id} не найден.", ex.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenValid_ResetsInKeycloak()
    {
        // Arrange
        var doctor = CreateDoctor();
        var dto = new ResetPasswordDto { NewPassword = "NewPass1!" };
        _doctors.Setup(r => r.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);

        // Act
        await _sut.ResetPasswordAsync(doctor.Id, dto, CancellationToken.None);

        // Assert
        _keycloak.Verify(
            k => k.ResetPasswordAsync(doctor.KeycloakId, dto.NewPassword, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
