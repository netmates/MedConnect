using AppointmentService.Application.DTOs.ScheduleSlot;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Services;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AppointmentService.UnitTests.Application;

public class ScheduleSlotApplicationServiceTests
{
    private readonly Mock<IScheduleSlotRepository> _slots = new();
    private readonly Mock<IDoctorRepository> _doctors = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IValidator<CreateScheduleSlotDto>> _createValidator = new();
    private readonly Mock<IValidator<UpdateScheduleSlotDto>> _updateValidator = new();

    private readonly ScheduleSlotApplicationService _sut;

    private static readonly DateTime FutureStart =
        new(2030, 6, 15, 10, 0, 0, DateTimeKind.Utc);

    public ScheduleSlotApplicationServiceTests()
    {
        _createValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateScheduleSlotDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _updateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateScheduleSlotDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new ScheduleSlotApplicationService(
            _slots.Object,
            _doctors.Object,
            _uow.Object,
            _createValidator.Object,
            _updateValidator.Object,
            NullLogger<ScheduleSlotApplicationService>.Instance);
    }

    private static Doctor CreateDoctor(string keycloakId = "doctor-kc")
        => Doctor.Create(keycloakId, "Петров", "Петр", "Петрович", "Терапевт", 10);

    private static ScheduleSlot CreateFutureSlot(Guid doctorId, int durationMinutes = 30)
        => ScheduleSlot.Create(doctorId, FutureStart, FutureStart.AddMinutes(durationMinutes));

    private static CreateScheduleSlotDto ValidCreateDto(DateTime? start = null, int durationMinutes = 30)
    {
        var startTime = start ?? FutureStart;
        return new CreateScheduleSlotDto
        {
            StartTime = startTime,
            EndTime = startTime.AddMinutes(durationMinutes)
        };
    }

    private static UpdateScheduleSlotDto ValidUpdateDto(DateTime? start = null, int durationMinutes = 45)
    {
        var startTime = start ?? FutureStart.AddHours(1);
        return new UpdateScheduleSlotDto
        {
            StartTime = startTime,
            EndTime = startTime.AddMinutes(durationMinutes)
        };
    }

    // Create

    [Fact]
    public async Task CreateAsync_WhenValidationFails_ThrowsValidationException()
    {
        // Arrange
        var failures = new[] { new ValidationFailure("StartTime", "Время начала должно быть в будущем.") };
        _createValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateScheduleSlotDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateAsync(ValidCreateDto(), "doctor-kc", CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WhenDoctorNotFound_ThrowsNotFound()
    {
        // Arrange
        _doctors.Setup(r => r.GetByKeycloakIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.CreateAsync(ValidCreateDto(), "missing", CancellationToken.None));

        // Assert
        Assert.Equal("Профиль врача не найден.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenDoctorInactive_ThrowsBusinessRule()
    {
        // Arrange
        var doctor = CreateDoctor();
        doctor.Deactivate();
        _doctors.Setup(r => r.GetByKeycloakIdAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(ValidCreateDto(), doctor.KeycloakId, CancellationToken.None));

        // Assert
        Assert.Equal("Нельзя управлять расписанием: профиль врача деактивирован.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenOverlaps_ThrowsBusinessRuleAndRollsBack()
    {
        // Arrange
        var doctor = CreateDoctor();
        var dto = ValidCreateDto();
        _doctors.Setup(r => r.GetByKeycloakIdAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _slots.Setup(r => r.HasOverlappingSlotAsync(
                doctor.Id, dto.StartTime, dto.EndTime, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(dto, doctor.KeycloakId, CancellationToken.None));

        // Assert
        Assert.Equal("Слот пересекается с существующим.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_AddsCommitsAndReturnsDto()
    {
        // Arrange
        var doctor = CreateDoctor();
        var dto = ValidCreateDto();
        _doctors.Setup(r => r.GetByKeycloakIdAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _slots.Setup(r => r.HasOverlappingSlotAsync(
                doctor.Id, dto.StartTime, dto.EndTime, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.CreateAsync(dto, doctor.KeycloakId, CancellationToken.None);

        // Assert
        Assert.Equal(doctor.Id, result.DoctorId);
        Assert.Equal(dto.StartTime, result.StartTime);
        Assert.Equal(dto.EndTime, result.EndTime);
        Assert.Equal(SlotStatus.Available.ToString(), result.Status);
        _slots.Verify(r => r.AddAsync(It.IsAny<ScheduleSlot>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Update

    [Fact]
    public async Task UpdateAsync_WhenValidationFails_ThrowsValidationException()
    {
        // Arrange
        var failures = new[] { new ValidationFailure("EndTime", "Время окончания должно быть позже времени начала.") };
        _updateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateScheduleSlotDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.UpdateAsync(Guid.NewGuid(), ValidUpdateDto(), "doctor-kc", CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WhenDoctorNotFound_ThrowsNotFound()
    {
        // Arrange
        _doctors.Setup(r => r.GetByKeycloakIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.UpdateAsync(Guid.NewGuid(), ValidUpdateDto(), "missing", CancellationToken.None));

        // Assert
        Assert.Equal("Профиль врача не найден.", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenDoctorInactive_ThrowsBusinessRule()
    {
        // Arrange
        var doctor = CreateDoctor();
        doctor.Deactivate();
        _doctors.Setup(r => r.GetByKeycloakIdAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.UpdateAsync(Guid.NewGuid(), ValidUpdateDto(), doctor.KeycloakId, CancellationToken.None));

        // Assert
        Assert.Equal("Нельзя управлять расписанием: профиль врача деактивирован.", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenSlotNotFound_ThrowsNotFoundAndRollsBack()
    {
        // Arrange
        var doctor = CreateDoctor();
        var id = Guid.NewGuid();
        _doctors.Setup(r => r.GetByKeycloakIdAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _slots.Setup(r => r.GetByIdWithLockAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScheduleSlot?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.UpdateAsync(id, ValidUpdateDto(), doctor.KeycloakId, CancellationToken.None));

        // Assert
        Assert.Equal($"Слот {id} не найден.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotOwner_ThrowsForbiddenAndRollsBack()
    {
        // Arrange
        var owner = CreateDoctor("owner-kc");
        var other = CreateDoctor("other-kc");
        var slot = CreateFutureSlot(owner.Id);
        _doctors.Setup(r => r.GetByKeycloakIdAsync(other.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(other);
        _slots.Setup(r => r.GetByIdWithLockAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);

        // Act
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.UpdateAsync(slot.Id, ValidUpdateDto(), other.KeycloakId, CancellationToken.None));

        // Assert
        Assert.Equal("Нет доступа к этому слоту.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenSlotNotAvailable_ThrowsBusinessRule()
    {
        // Arrange
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        slot.Book();
        _doctors.Setup(r => r.GetByKeycloakIdAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _slots.Setup(r => r.GetByIdWithLockAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.UpdateAsync(slot.Id, ValidUpdateDto(), doctor.KeycloakId, CancellationToken.None));

        // Assert
        Assert.Equal("Нельзя редактировать слот: он уже забронирован.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenOverlaps_ThrowsBusinessRuleAndRollsBack()
    {
        // Arrange
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        var dto = ValidUpdateDto();
        _doctors.Setup(r => r.GetByKeycloakIdAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _slots.Setup(r => r.GetByIdWithLockAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);
        _slots.Setup(r => r.HasOverlappingSlotAsync(
                doctor.Id, dto.StartTime, dto.EndTime, slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.UpdateAsync(slot.Id, dto, doctor.KeycloakId, CancellationToken.None));

        // Assert
        Assert.Equal("Слот пересекается с существующим расписанием.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesCommitsAndReturnsDto()
    {
        // Arrange
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        var dto = ValidUpdateDto();
        _doctors.Setup(r => r.GetByKeycloakIdAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _slots.Setup(r => r.GetByIdWithLockAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);
        _slots.Setup(r => r.HasOverlappingSlotAsync(
                doctor.Id, dto.StartTime, dto.EndTime, slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.UpdateAsync(slot.Id, dto, doctor.KeycloakId, CancellationToken.None);

        // Assert
        Assert.Equal(dto.StartTime, result.StartTime);
        Assert.Equal(dto.EndTime, result.EndTime);
        _slots.Verify(r => r.UpdateAsync(slot, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Delete

    [Fact]
    public async Task DeleteAsync_WhenDoctorNotFound_ThrowsNotFound()
    {
        // Arrange
        _doctors.Setup(r => r.GetByKeycloakIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.DeleteAsync(Guid.NewGuid(), "missing", CancellationToken.None));

        // Assert
        Assert.Equal("Профиль врача не найден.", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_WhenDoctorInactive_ThrowsBusinessRule()
    {
        // Arrange
        var doctor = CreateDoctor();
        doctor.Deactivate();
        _doctors.Setup(r => r.GetByKeycloakIdAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.DeleteAsync(Guid.NewGuid(), doctor.KeycloakId, CancellationToken.None));

        // Assert
        Assert.Equal("Нельзя управлять расписанием: профиль врача деактивирован.", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_WhenSlotNotFound_ThrowsNotFoundAndRollsBack()
    {
        // Arrange
        var doctor = CreateDoctor();
        var id = Guid.NewGuid();
        _doctors.Setup(r => r.GetByKeycloakIdAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _slots.Setup(r => r.GetByIdWithLockAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScheduleSlot?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.DeleteAsync(id, doctor.KeycloakId, CancellationToken.None));

        // Assert
        Assert.Equal($"Слот {id} не найден.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotOwner_ThrowsForbiddenAndRollsBack()
    {
        // Arrange
        var owner = CreateDoctor("owner-kc");
        var other = CreateDoctor("other-kc");
        var slot = CreateFutureSlot(owner.Id);
        _doctors.Setup(r => r.GetByKeycloakIdAsync(other.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(other);
        _slots.Setup(r => r.GetByIdWithLockAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);

        // Act
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.DeleteAsync(slot.Id, other.KeycloakId, CancellationToken.None));

        // Assert
        Assert.Equal("Нет доступа к этому слоту.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenSlotNotAvailable_ThrowsBusinessRule()
    {
        // Arrange
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        slot.Book();
        _doctors.Setup(r => r.GetByKeycloakIdAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _slots.Setup(r => r.GetByIdWithLockAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.DeleteAsync(slot.Id, doctor.KeycloakId, CancellationToken.None));

        // Assert
        Assert.Equal("Удалить можно только свободный слот.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenValid_DeletesAndCommits()
    {
        // Arrange
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        _doctors.Setup(r => r.GetByKeycloakIdAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _slots.Setup(r => r.GetByIdWithLockAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);

        // Act
        await _sut.DeleteAsync(slot.Id, doctor.KeycloakId, CancellationToken.None);

        // Assert
        _slots.Verify(r => r.DeleteAsync(slot, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // GetByDoctorId

    [Fact]
    public async Task GetByDoctorIdAsync_WhenDoctorNotFound_ThrowsNotFound()
    {
        // Arrange
        var doctorId = Guid.NewGuid();
        _doctors.Setup(r => r.GetByIdAsync(doctorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetByDoctorIdAsync(doctorId, CancellationToken.None));

        // Assert
        Assert.Equal("Врач не найден.", ex.Message);
    }

    [Fact]
    public async Task GetByDoctorIdAsync_WhenExists_ReturnsMappedList()
    {
        // Arrange
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        _doctors.Setup(r => r.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _slots.Setup(r => r.GetByDoctorIdAsync(doctor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([slot]);

        // Act
        var result = await _sut.GetByDoctorIdAsync(doctor.Id, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(slot.Id, result[0].Id);
        Assert.Equal(SlotStatus.Available.ToString(), result[0].Status);
    }

    // GetAvailable

    [Fact]
    public async Task GetAvailableAsync_WhenDoctorNotFound_ThrowsNotFound()
    {
        // Arrange
        var doctorId = Guid.NewGuid();
        var date = FutureStart.Date;
        _doctors.Setup(r => r.GetByIdAsync(doctorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetAvailableAsync(doctorId, date, CancellationToken.None));

        // Assert
        Assert.Equal("Врач не найден.", ex.Message);
    }

    [Fact]
    public async Task GetAvailableAsync_WhenExists_ReturnsMappedList()
    {
        // Arrange
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        var date = FutureStart.Date;
        _doctors.Setup(r => r.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _slots.Setup(r => r.GetAvailableByDoctorIdAsync(doctor.Id, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync([slot]);

        // Act
        var result = await _sut.GetAvailableAsync(doctor.Id, date, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(slot.Id, result[0].Id);
        Assert.Equal(doctor.Id, result[0].DoctorId);
    }
}
