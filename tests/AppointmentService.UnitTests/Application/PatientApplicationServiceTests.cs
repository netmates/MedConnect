using AppointmentService.Application.Auth;
using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Application.Services;
using AppointmentService.Domain.Entities;
using FluentValidation;
using MedConnect.Shared.Messaging;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;

namespace AppointmentService.UnitTests.Application;

public class PatientApplicationServiceTests
{
    private readonly Mock<IPatientRepository> _patients = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IKeycloakAdminService> _keycloak = new();
    private readonly Mock<IValidator<SignUpPatientDto>> _signUpValidator = new();
    private readonly Mock<IValidator<UpdatePatientDto>> _updateValidator = new();
    private readonly Mock<IIntegrationEventPublisher> _publisher = new();
    private readonly Mock<ICorrelationIdAccessor> _correlationIdAccessor = new();

    private readonly PatientApplicationService _sut;

    public PatientApplicationServiceTests()
    {
        _signUpValidator
            .Setup(v => v.ValidateAsync(It.IsAny<SignUpPatientDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _updateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdatePatientDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new PatientApplicationService(
            _patients.Object,
            _uow.Object,
            _keycloak.Object,
            _signUpValidator.Object,
            _updateValidator.Object,
            NullLogger<PatientApplicationService>.Instance,
            _publisher.Object,
            _correlationIdAccessor.Object);
    }

    private static Patient CreatePatient(string keycloakId = "patient-kc")
        => Patient.Create(
            keycloakId,
            "Иванов",
            "Иван",
            "Иванович",
            "+79001234567",
            new DateTime(1990, 1, 1));

    private static SignUpPatientDto ValidSignUpDto() => new()
    {
        Email = "patient@medconnect.local",
        Password = "Patient1Pass!",
        LastName = "Иванов",
        FirstName = "Иван",
        MiddleName = "Иванович",
        Phone = "+79001234567",
        DateOfBirth = new DateTime(1990, 1, 1)
    };

    private static UpdatePatientDto ValidUpdateDto() => new()
    {
        LastName = "Сидоров",
        FirstName = "Сидор",
        MiddleName = "Сидорович",
        Phone = "+79007654321",
        DateOfBirth = new DateTime(1985, 3, 10)
    };

    // SignUp

    [Fact]
    public async Task SignUpAsync_WhenValidationFails_ThrowsValidationException()
    {
        // Arrange
        var failures = new[] { new ValidationFailure("LastName", "Фамилия обязательна.") };
        _signUpValidator
            .Setup(v => v.ValidateAsync(It.IsAny<SignUpPatientDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.SignUpAsync(ValidSignUpDto(), CancellationToken.None));
        _keycloak.Verify(
            k => k.CreateUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SignUpAsync_CreatesUserCommitsAndReturnsDto()
    {
        // Arrange
        const string keycloakId = "new-patient-kc";
        var dto = ValidSignUpDto();
        SetupCreateUser(keycloakId);

        // Act
        var result = await _sut.SignUpAsync(dto, CancellationToken.None);

        // Assert
        Assert.Equal(keycloakId, result.KeycloakId);
        Assert.Equal(dto.LastName, result.LastName);
        Assert.Equal(dto.FirstName, result.FirstName);
        Assert.Equal(dto.MiddleName, result.MiddleName);
        Assert.Equal(dto.Phone, result.Phone);
        Assert.True(result.IsActive);
        _keycloak.Verify(
            k => k.CreateUserAsync(
                dto.Email, dto.Password, Roles.Patient,
                dto.FirstName, dto.LastName, false, It.IsAny<CancellationToken>()),
            Times.Once);
        _patients.Verify(
            r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _keycloak.Verify(
            k => k.DeleteUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SignUpAsync_WhenAddFails_RollsBackAndDeletesKeycloakUser()
    {
        // Arrange
        const string keycloakId = "new-patient-kc";
        SetupCreateUser(keycloakId);
        _patients.Setup(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db error"));

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.SignUpAsync(ValidSignUpDto(), CancellationToken.None));

        // Assert
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _keycloak.Verify(k => k.DeleteUserAsync(keycloakId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignUpAsync_WhenUniqueViolationOnCommit_RollsBackDeletesUserAndThrows()
    {
        // Arrange
        const string keycloakId = "race-patient-kc";
        var dto = ValidSignUpDto();
        SetupCreateUser(keycloakId);

        var postgresEx = new PostgresException(
            messageText: "duplicate key value violates unique constraint",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: PostgresErrorCodes.UniqueViolation);

        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique violation", postgresEx));

        // Act
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() =>
            _sut.SignUpAsync(dto, CancellationToken.None));

        // Assert
        Assert.Same(postgresEx, ex.InnerException);
        _patients.Verify(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _keycloak.Verify(k => k.DeleteUserAsync(keycloakId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupCreateUser(string keycloakId)
    {
        _keycloak
            .Setup(k => k.CreateUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(keycloakId);
    }

    // GetByKeycloakId

    [Fact]
    public async Task GetByKeycloakIdAsync_WhenNotFound_ThrowsNotFound()
    {
        // Arrange
        _patients.Setup(r => r.GetByKeycloakIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetByKeycloakIdAsync("missing", CancellationToken.None));

        // Assert
        Assert.Equal("Профиль пациента не найден.", ex.Message);
    }

    [Fact]
    public async Task GetByKeycloakIdAsync_WhenExists_ReturnsDto()
    {
        // Arrange
        var patient = CreatePatient();
        _patients.Setup(r => r.GetByKeycloakIdAsync(patient.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);

        // Act
        var result = await _sut.GetByKeycloakIdAsync(patient.KeycloakId, CancellationToken.None);

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
        var failures = new[] { new ValidationFailure("FirstName", "Имя обязательно.") };
        _updateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdatePatientDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.UpdateAsync("patient-kc", ValidUpdateDto(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WhenPatientNotFound_ThrowsNotFound()
    {
        // Arrange
        _patients.Setup(r => r.GetByKeycloakIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.UpdateAsync("missing", ValidUpdateDto(), CancellationToken.None));

        // Assert
        Assert.Equal("Профиль пациента не найден.", ex.Message);
        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        _keycloak.Verify(
            k => k.UpdateUserNameAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesCommitsAndReturnsDto()
    {
        // Arrange
        var patient = CreatePatient();
        var dto = ValidUpdateDto();
        _patients.Setup(r => r.GetByKeycloakIdAsync(patient.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);

        // Act
        var result = await _sut.UpdateAsync(patient.KeycloakId, dto, CancellationToken.None);

        // Assert
        Assert.Equal("Сидоров", result.LastName);
        Assert.Equal("Сидор", result.FirstName);
        Assert.Equal("Сидорович", result.MiddleName);
        Assert.Equal("+79007654321", result.Phone);
        _patients.Verify(r => r.UpdateAsync(patient, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNameChanged_UpdatesKeycloak()
    {
        // Arrange
        var patient = CreatePatient();
        var dto = ValidUpdateDto();
        _patients.Setup(r => r.GetByKeycloakIdAsync(patient.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);

        // Act
        await _sut.UpdateAsync(patient.KeycloakId, dto, CancellationToken.None);

        // Assert
        _keycloak.Verify(
            k => k.UpdateUserNameAsync(
                patient.KeycloakId, "Сидор", "Сидоров", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenOnlyPhoneChanged_DoesNotCallKeycloak()
    {
        // Arrange
        var patient = CreatePatient();
        var dto = new UpdatePatientDto
        {
            LastName = patient.LastName,
            FirstName = patient.FirstName,
            MiddleName = patient.MiddleName,
            Phone = "+79009998877",
            DateOfBirth = patient.DateOfBirth
        };
        _patients.Setup(r => r.GetByKeycloakIdAsync(patient.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);

        // Act
        await _sut.UpdateAsync(patient.KeycloakId, dto, CancellationToken.None);

        // Assert
        _keycloak.Verify(
            k => k.UpdateUserNameAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenDbFailsAfterKeycloak_CompensatesOldName()
    {
        // Arrange
        var patient = CreatePatient();
        var dto = ValidUpdateDto();
        _patients.Setup(r => r.GetByKeycloakIdAsync(patient.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db"));

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateAsync(patient.KeycloakId, dto, CancellationToken.None));

        // Assert
        _keycloak.Verify(
            k => k.UpdateUserNameAsync(
                patient.KeycloakId, "Сидор", "Сидоров", It.IsAny<CancellationToken>()),
            Times.Once);
        _keycloak.Verify(
            k => k.UpdateUserNameAsync(
                patient.KeycloakId, "Иван", "Иванов", It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
