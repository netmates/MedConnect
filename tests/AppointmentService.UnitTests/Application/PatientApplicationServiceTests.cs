using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Services;
using AppointmentService.Domain.Entities;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace AppointmentService.UnitTests.Application;

public class PatientApplicationServiceTests
{
    private readonly Mock<IPatientRepository> _patients = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IValidator<RegisterPatientDto>> _registerValidator = new();
    private readonly Mock<IValidator<UpdatePatientDto>> _updateValidator = new();

    private readonly PatientApplicationService _sut;

    public PatientApplicationServiceTests()
    {
        _registerValidator
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterPatientDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _updateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdatePatientDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new PatientApplicationService(
            _patients.Object,
            _uow.Object,
            _registerValidator.Object,
            _updateValidator.Object);
    }

    private static Patient CreatePatient(string keycloakId = "patient-kc")
        => Patient.Create(
            keycloakId,
            "Иванов",
            "Иван",
            "Иванович",
            "+79001234567",
            new DateTime(1990, 1, 1));

    private static RegisterPatientDto ValidRegisterDto()
        => new()
        {
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            Phone = "+79001234567",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

    private static UpdatePatientDto ValidUpdateDto()
        => new()
        {
            LastName = "Сидоров",
            FirstName = "Сидор",
            MiddleName = "Сидорович",
            Phone = "+79007654321",
            DateOfBirth = new DateTime(1985, 3, 10)
        };

    // RegisterOrGet

    [Fact]
    public async Task RegisterOrGetAsync_WhenValidationFails_ThrowsValidationException()
    {
        // Arrange
        var failures = new[] { new ValidationFailure("LastName", "Фамилия обязательна.") };
        _registerValidator
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterPatientDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RegisterOrGetAsync("patient-kc", ValidRegisterDto(), CancellationToken.None));
    }

    [Fact]
    public async Task RegisterOrGetAsync_WhenPatientExists_ReturnsExistingWithoutCreating()
    {
        // Arrange
        var existing = CreatePatient();
        _patients.Setup(r => r.GetByKeycloakIdAsync(existing.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _sut.RegisterOrGetAsync(
            existing.KeycloakId, ValidRegisterDto(), CancellationToken.None);

        // Assert
        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(existing.KeycloakId, result.KeycloakId);
        _patients.Verify(
            r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterOrGetAsync_WhenPatientNotExists_CreatesCommitsAndReturnsDto()
    {
        // Arrange
        const string keycloakId = "new-patient-kc";
        var dto = ValidRegisterDto();
        _patients.Setup(r => r.GetByKeycloakIdAsync(keycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        // Act
        var result = await _sut.RegisterOrGetAsync(keycloakId, dto, CancellationToken.None);

        // Assert
        Assert.Equal(keycloakId, result.KeycloakId);
        Assert.Equal(dto.LastName, result.LastName);
        Assert.Equal(dto.FirstName, result.FirstName);
        Assert.Equal(dto.MiddleName, result.MiddleName);
        Assert.Equal(dto.Phone, result.Phone);
        Assert.True(result.IsActive);
        _patients.Verify(
            r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterOrGetAsync_WhenAddFails_RollsBack()
    {
        // Arrange
        const string keycloakId = "new-patient-kc";
        _patients.Setup(r => r.GetByKeycloakIdAsync(keycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);
        _patients.Setup(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db error"));

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.RegisterOrGetAsync(keycloakId, ValidRegisterDto(), CancellationToken.None));

        // Assert
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
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
    public async Task UpdateAsync_WhenPatientNotFound_ThrowsNotFoundAndRollsBack()
    {
        // Arrange
        _patients.Setup(r => r.GetByKeycloakIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.UpdateAsync("missing", ValidUpdateDto(), CancellationToken.None));

        // Assert
        Assert.Equal("Профиль пациента не найден.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
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
}
