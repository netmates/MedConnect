using AppointmentService.Application.DTOs.Specialization;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Services;
using AppointmentService.Domain.Entities;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AppointmentService.UnitTests.Application;

public class SpecializationApplicationServiceTests
{
    private readonly Mock<ISpecializationRepository> _specializations = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IValidator<CreateSpecializationDto>> _createValidator = new();
    private readonly Mock<IValidator<UpdateSpecializationDto>> _updateValidator = new();

    private readonly SpecializationApplicationService _sut;

    public SpecializationApplicationServiceTests()
    {
        _createValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateSpecializationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _updateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateSpecializationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new SpecializationApplicationService(
            _specializations.Object,
            _uow.Object,
            _createValidator.Object,
            _updateValidator.Object,
            NullLogger<SpecializationApplicationService>.Instance);
    }

    private static Specialization CreateSpecialization(string name = "Терапия")
        => Specialization.Create(name);

    private static CreateSpecializationDto ValidCreateDto(string name = "Кардиология")
        => new() { Name = name };

    private static UpdateSpecializationDto ValidUpdateDto(string name = "Хирургия")
        => new() { Name = name };

    // GetAll

    [Fact]
    public async Task GetAllAsync_ReturnsMappedList()
    {
        // Arrange
        var specialization = CreateSpecialization();
        _specializations.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([specialization]);

        // Act
        var result = await _sut.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(specialization.Id, result[0].Id);
        Assert.Equal(specialization.Name, result[0].Name);
    }

    // Create

    [Fact]
    public async Task CreateAsync_WhenValidationFails_ThrowsValidationException()
    {
        // Arrange
        var failures = new[] { new ValidationFailure("Name", "Название специализации обязательно.") };
        _createValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateSpecializationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateAsync(ValidCreateDto(), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WhenValid_AddsCommitsAndReturnsDto()
    {
        // Arrange
        var dto = ValidCreateDto("Неврология");

        // Act
        var result = await _sut.CreateAsync(dto, CancellationToken.None);

        // Assert
        Assert.Equal("Неврология", result.Name);
        Assert.NotEqual(Guid.Empty, result.Id);
        _specializations.Verify(
            r => r.AddAsync(It.IsAny<Specialization>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenAddFails_RollsBack()
    {
        // Arrange
        _specializations.Setup(r => r.AddAsync(It.IsAny<Specialization>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db error"));

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.CreateAsync(ValidCreateDto(), CancellationToken.None));

        // Assert
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Update

    [Fact]
    public async Task UpdateAsync_WhenValidationFails_ThrowsValidationException()
    {
        // Arrange
        var failures = new[] { new ValidationFailure("Name", "Название специализации обязательно.") };
        _updateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateSpecializationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.UpdateAsync(Guid.NewGuid(), ValidUpdateDto(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ThrowsNotFoundAndRollsBack()
    {
        // Arrange
        var id = Guid.NewGuid();
        _specializations.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Specialization?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.UpdateAsync(id, ValidUpdateDto(), CancellationToken.None));

        // Assert
        Assert.Equal($"Специализация {id} не найдена.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesCommitsAndReturnsDto()
    {
        // Arrange
        var specialization = CreateSpecialization("Терапия");
        var dto = ValidUpdateDto("Хирургия");
        _specializations.Setup(r => r.GetByIdAsync(specialization.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(specialization);

        // Act
        var result = await _sut.UpdateAsync(specialization.Id, dto, CancellationToken.None);

        // Assert
        Assert.Equal("Хирургия", result.Name);
        Assert.Equal(specialization.Id, result.Id);
        _specializations.Verify(
            r => r.UpdateAsync(specialization, It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Delete

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ThrowsNotFoundAndRollsBack()
    {
        // Arrange
        var id = Guid.NewGuid();
        _specializations.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Specialization?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.DeleteAsync(id, CancellationToken.None));

        // Assert
        Assert.Equal($"Специализация {id} не найдена.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenHasLinkedDoctors_ThrowsBusinessRuleAndRollsBack()
    {
        // Arrange
        var specialization = CreateSpecialization();
        _specializations.Setup(r => r.GetByIdAsync(specialization.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(specialization);
        _specializations.Setup(r => r.HasAnyDoctorsAsync(specialization.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.DeleteAsync(specialization.Id, CancellationToken.None));

        // Assert
        Assert.Equal("Нельзя удалить специализацию: к ней привязаны врачи.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _specializations.Verify(
            r => r.DeleteAsync(It.IsAny<Specialization>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenValid_DeletesAndCommits()
    {
        // Arrange
        var specialization = CreateSpecialization();
        _specializations.Setup(r => r.GetByIdAsync(specialization.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(specialization);
        _specializations.Setup(r => r.HasAnyDoctorsAsync(specialization.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _sut.DeleteAsync(specialization.Id, CancellationToken.None);

        // Assert
        _specializations.Verify(
            r => r.DeleteAsync(specialization, It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
