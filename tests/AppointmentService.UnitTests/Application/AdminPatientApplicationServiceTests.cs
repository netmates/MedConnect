using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Application.Services;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace AppointmentService.UnitTests.Application;

public class AdminPatientApplicationServiceTests
{
    private readonly Mock<IPatientRepository> _patients = new();
    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly Mock<IScheduleSlotRepository> _slots = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IKeycloakAdminService> _keycloak = new();
    private readonly Mock<IValidator<UpdatePatientDto>> _updateValidator = new();

    private readonly AdminPatientApplicationService _sut;

    private static readonly DateTime FutureStart =
        new(2030, 6, 15, 10, 0, 0, DateTimeKind.Utc);

    public AdminPatientApplicationServiceTests()
    {
        _updateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdatePatientDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new AdminPatientApplicationService(
            _patients.Object,
            _appointments.Object,
            _slots.Object,
            _uow.Object,
            _keycloak.Object,
            _updateValidator.Object);
    }

    private static Patient CreatePatient(string keycloakId = "patient-kc")
        => Patient.Create(keycloakId, "Иванов", "Иван", "Иванович", "+79001234567", new DateTime(1990, 1, 1));

    private static Doctor CreateDoctor(string keycloakId = "doctor-kc")
        => Doctor.Create(keycloakId, "Петров", "Петр", "Петрович", "Терапевт", 10);

    private static ScheduleSlot CreateFutureSlot(Guid doctorId)
        => ScheduleSlot.Create(doctorId, FutureStart, FutureStart.AddMinutes(30));

    private static UpdatePatientDto ValidUpdateDto()
        => new()
        {
            LastName = "Сидоров",
            FirstName = "Сидор",
            MiddleName = "Сидорович",
            Phone = "+79007654321",
            DateOfBirth = new DateTime(1985, 3, 10)
        };

    // GetAll

    [Fact]
    public async Task GetAllIncludingInactiveAsync_ReturnsMappedList()
    {
        // Arrange
        var patient = CreatePatient();
        patient.Deactivate();
        _patients.Setup(r => r.GetAllIncludingInactiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([patient]);

        // Act
        var result = await _sut.GetAllIncludingInactiveAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(patient.Id, result[0].Id);
        Assert.False(result[0].IsActive);
        Assert.Equal(patient.KeycloakId, result[0].KeycloakId);
    }

    // GetById

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ThrowsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _patients.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetByIdAsync(id, CancellationToken.None));

        // Assert
        Assert.Equal($"Пациент {id} не найден.", ex.Message);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var patient = CreatePatient();
        _patients.Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);

        // Act
        var result = await _sut.GetByIdAsync(patient.Id, CancellationToken.None);

        // Assert
        Assert.Equal(patient.Id, result.Id);
        Assert.Equal("Иванов", result.LastName);
        Assert.Equal("Иван", result.FirstName);
        Assert.True(result.IsActive);
    }

    // Update

    [Fact]
    public async Task UpdateAsync_WhenValidationFails_ThrowsValidationException()
    {
        // Arrange
        var failures = new[] { new ValidationFailure("LastName", "Фамилия обязательна.") };
        _updateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdatePatientDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.UpdateAsync(Guid.NewGuid(), ValidUpdateDto(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WhenPatientNotFound_ThrowsNotFoundAndRollsBack()
    {
        // Arrange
        var id = Guid.NewGuid();
        _patients.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.UpdateAsync(id, ValidUpdateDto(), CancellationToken.None));

        // Assert
        Assert.Equal($"Пациент {id} не найден.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesCommitsAndReturnsDto()
    {
        // Arrange
        var patient = CreatePatient();
        var dto = ValidUpdateDto();
        _patients.Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);

        // Act
        var result = await _sut.UpdateAsync(patient.Id, dto, CancellationToken.None);

        // Assert
        Assert.Equal("Сидоров", result.LastName);
        Assert.Equal("Сидор", result.FirstName);
        Assert.Equal("Сидорович", result.MiddleName);
        Assert.Equal("+79007654321", result.Phone);
        _patients.Verify(r => r.UpdateAsync(patient, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Deactivate

    [Fact]
    public async Task DeactivateAsync_WhenPatientNotFound_ThrowsNotFoundAndRollsBack()
    {
        // Arrange
        var id = Guid.NewGuid();
        _patients.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.DeactivateAsync(id, CancellationToken.None));

        // Assert
        Assert.Equal($"Пациент {id} не найден.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _keycloak.Verify(
            k => k.DisableUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeactivateAsync_WhenNoActiveAppointments_DeactivatesAndDisablesInKeycloak()
    {
        // Arrange
        var patient = CreatePatient();
        _patients.Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _appointments.Setup(r => r.GetActiveFutureByPatientIdAsync(
                patient.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await _sut.DeactivateAsync(patient.Id, CancellationToken.None);

        // Assert
        Assert.False(patient.IsActive);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _keycloak.Verify(
            k => k.DisableUserAsync(patient.KeycloakId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_WhenHasActiveAppointments_CancelsThemAndFreesSlots()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        slot.Book();
        var appointment = Appointment.Create(patient.Id, doctor.Id, slot.Id, "Осмотр");

        _patients.Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _appointments.Setup(r => r.GetActiveFutureByPatientIdAsync(
                patient.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([appointment]);
        _appointments.Setup(r => r.GetByIdWithLockAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);
        _slots.Setup(r => r.GetByIdWithLockAsync(appointment.SlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);

        // Act
        await _sut.DeactivateAsync(patient.Id, CancellationToken.None);

        // Assert
        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Equal(SlotStatus.Available, slot.Status);
        Assert.False(patient.IsActive);
        _appointments.Verify(r => r.UpdateAsync(appointment, It.IsAny<CancellationToken>()), Times.Once);
        _slots.Verify(r => r.UpdateAsync(slot, It.IsAny<CancellationToken>()), Times.Once);
        _keycloak.Verify(
            k => k.DisableUserAsync(patient.KeycloakId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_WhenSlotNotFound_ThrowsNotFoundAndRollsBack()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        slot.Book();
        var appointment = Appointment.Create(patient.Id, doctor.Id, slot.Id, null);

        _patients.Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _appointments.Setup(r => r.GetActiveFutureByPatientIdAsync(
                patient.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([appointment]);
        _appointments.Setup(r => r.GetByIdWithLockAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);
        _slots.Setup(r => r.GetByIdWithLockAsync(appointment.SlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScheduleSlot?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.DeactivateAsync(patient.Id, CancellationToken.None));

        // Assert
        Assert.Equal("Слот записи не найден.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _keycloak.Verify(
            k => k.DisableUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Activate

    [Fact]
    public async Task ActivateAsync_WhenPatientNotFound_ThrowsNotFoundAndRollsBack()
    {
        // Arrange
        var id = Guid.NewGuid();
        _patients.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.ActivateAsync(id, CancellationToken.None));

        // Assert
        Assert.Equal($"Пациент {id} не найден.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _keycloak.Verify(
            k => k.EnableUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ActivateAsync_WhenExists_ActivatesAndEnablesInKeycloak()
    {
        // Arrange
        var patient = CreatePatient();
        patient.Deactivate();
        _patients.Setup(r => r.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);

        // Act
        await _sut.ActivateAsync(patient.Id, CancellationToken.None);

        // Assert
        Assert.True(patient.IsActive);
        _patients.Verify(r => r.UpdateAsync(patient, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _keycloak.Verify(
            k => k.EnableUserAsync(patient.KeycloakId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
