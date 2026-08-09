using AppointmentService.Application.DTOs.Appointment;
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
using System.Reflection;

namespace AppointmentService.UnitTests.Application;

public class AppointmentApplicationServiceTests
{
    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly Mock<IScheduleSlotRepository> _slots = new();
    private readonly Mock<IPatientRepository> _patients = new();
    private readonly Mock<IDoctorRepository> _doctors = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IValidator<CreateAppointmentDto>> _createValidator = new();

    private readonly AppointmentApplicationService _sut;

    private static readonly DateTime FutureStart =
        new(2030, 6, 15, 10, 0, 0, DateTimeKind.Utc);

    public AppointmentApplicationServiceTests()
    {
        _createValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateAppointmentDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new AppointmentApplicationService(
            _appointments.Object,
            _slots.Object,
            _patients.Object,
            _doctors.Object,
            _uow.Object,
            _createValidator.Object,
            NullLogger<AppointmentApplicationService>.Instance);
    }

    private static Patient CreatePatient(string keycloakId = "patient-kc")
        => Patient.Create(keycloakId, "Иванов", "Иван", "Иванович", "+79001234567", new DateTime(1990, 1, 1));

    private static Doctor CreateDoctor(string keycloakId = "doctor-kc")
        => Doctor.Create(keycloakId, "Петров", "Петр", "Петрович", "Терапевт", 10);

    private static ScheduleSlot CreateFutureSlot(Guid doctorId, int durationMinutes = 30)
        => ScheduleSlot.Create(doctorId, FutureStart, FutureStart.AddMinutes(durationMinutes));

    private static ScheduleSlot CreatePastSlot(Guid doctorId)
    {
        var start = DateTime.UtcNow.AddHours(-2);
        return ScheduleSlot.Create(doctorId, start, start.AddMinutes(30));
    }

    private static Appointment CreateAppointmentWithDetails(
        Patient patient,
        Doctor doctor,
        ScheduleSlot slot,
        string? reason = "Осмотр")
    {
        var appointment = Appointment.Create(patient.Id, doctor.Id, slot.Id, reason);
        AttachDetails(appointment, patient, doctor, slot);
        return appointment;
    }

    private static void AttachDetails(
        Appointment appointment,
        Patient patient,
        Doctor doctor,
        ScheduleSlot slot)
    {
        SetNav(appointment, nameof(Appointment.Patient), patient);
        SetNav(appointment, nameof(Appointment.Doctor), doctor);
        SetNav(appointment, nameof(Appointment.Slot), slot);
    }

    private static void SetNav(Appointment appointment, string propertyName, object value)
        => typeof(Appointment)
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(appointment, value);

    // GetByPatient

    [Fact]
    public async Task GetByPatientAsync_WhenPatientNotFound_ThrowsNotFound()
    {
        // Arrange
        _patients.Setup(r => r.GetByKeycloakIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetByPatientAsync("missing", null, null, null, CancellationToken.None));

        // Assert
        Assert.Equal("Пациент не найден.", ex.Message);
    }

    [Fact]
    public async Task GetByPatientAsync_WhenPatientExists_ReturnsMappedList()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        var appointment = CreateAppointmentWithDetails(patient, doctor, slot);

        _patients.Setup(r => r.GetByKeycloakIdAsync(patient.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _appointments.Setup(r => r.GetByPatientIdAsync(
                patient.Id, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([appointment]);

        // Act
        var result = await _sut.GetByPatientAsync(
            patient.KeycloakId, null, null, null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(appointment.Id, result[0].Id);
        Assert.Equal("Иванов Иван Иванович", result[0].PatientFullName);
        Assert.Equal("Петров Петр Петрович", result[0].DoctorFullName);
        Assert.Equal(AppointmentStatus.Created.ToString(), result[0].Status);
    }

    // GetByDoctor

    [Fact]
    public async Task GetByDoctorAsync_WhenDoctorNotFound_ThrowsNotFound()
    {
        // Arrange
        _doctors.Setup(r => r.GetByKeycloakIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetByDoctorAsync("missing", null, null, null, CancellationToken.None));

        // Assert
        Assert.Equal("Врач не найден.", ex.Message);
    }

    [Fact]
    public async Task GetByDoctorAsync_WhenDoctorExists_ReturnsMappedList()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        var appointment = CreateAppointmentWithDetails(patient, doctor, slot);

        _doctors.Setup(r => r.GetByKeycloakIdAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _appointments.Setup(r => r.GetByDoctorIdAsync(
                doctor.Id, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([appointment]);

        // Act
        var result = await _sut.GetByDoctorAsync(
            doctor.KeycloakId, null, null, null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(appointment.Id, result[0].Id);
    }

    // GetById

    [Fact]
    public async Task GetByIdAsync_WhenAppointmentNotFound_ThrowsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _appointments.Setup(r => r.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetByIdAsync(id, "any", CancellationToken.None));

        // Assert
        Assert.Equal("Запись не найдена.", ex.Message);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCallerIsPatientOwner_ReturnsDto()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        var appointment = CreateAppointmentWithDetails(patient, doctor, slot);

        _appointments.Setup(r => r.GetByIdWithDetailsAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);
        _patients.Setup(r => r.GetByKeycloakIdAsync(patient.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);

        // Act
        var result = await _sut.GetByIdAsync(
            appointment.Id, patient.KeycloakId, CancellationToken.None);

        // Assert
        Assert.Equal(appointment.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCallerIsDoctorOwner_ReturnsDto()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        var appointment = CreateAppointmentWithDetails(patient, doctor, slot);

        _appointments.Setup(r => r.GetByIdWithDetailsAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);
        _patients.Setup(r => r.GetByKeycloakIdAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);
        _doctors.Setup(r => r.GetByKeycloakIdAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);

        // Act
        var result = await _sut.GetByIdAsync(
            appointment.Id, doctor.KeycloakId, CancellationToken.None);

        // Assert
        Assert.Equal(appointment.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCallerIsNeitherOwner_ThrowsForbidden()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        var appointment = CreateAppointmentWithDetails(patient, doctor, slot);
        var stranger = CreatePatient("stranger-kc");

        _appointments.Setup(r => r.GetByIdWithDetailsAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);
        _patients.Setup(r => r.GetByKeycloakIdAsync(stranger.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stranger);
        _doctors.Setup(r => r.GetByKeycloakIdAsync(stranger.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        // Act
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.GetByIdAsync(appointment.Id, stranger.KeycloakId, CancellationToken.None));

        // Assert
        Assert.Equal("Нет доступа к этой записи.", ex.Message);
    }

    // Create

    [Fact]
    public async Task CreateAsync_WhenValidationFails_ThrowsValidationException()
    {
        // Arrange
        var failures = new[] { new ValidationFailure("SlotId", "SlotId обязателен.") };
        _createValidator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateAppointmentDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateAsync(new CreateAppointmentDto { SlotId = Guid.Empty }, "p", CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WhenPatientNotFound_ThrowsNotFound()
    {
        // Arrange
        _patients.Setup(r => r.GetByKeycloakIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.CreateAsync(
                new CreateAppointmentDto { SlotId = Guid.NewGuid() },
                "missing",
                CancellationToken.None));

        // Assert
        Assert.Equal("Пациент не найден.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenPatientInactive_ThrowsBusinessRule()
    {
        // Arrange
        var patient = CreatePatient();
        patient.Deactivate();
        _patients.Setup(r => r.GetByKeycloakIdAsync(patient.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(
                new CreateAppointmentDto { SlotId = Guid.NewGuid() },
                patient.KeycloakId,
                CancellationToken.None));

        // Assert
        Assert.Equal("Нельзя записаться: профиль пациента деактивирован.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenSlotNotFound_ThrowsNotFoundAndRollsBack()
    {
        // Arrange
        var patient = CreatePatient();
        var slotId = Guid.NewGuid();
        _patients.Setup(r => r.GetByKeycloakIdAsync(patient.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _slots.Setup(r => r.GetByIdWithLockAsync(slotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScheduleSlot?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.CreateAsync(
                new CreateAppointmentDto { SlotId = slotId },
                patient.KeycloakId,
                CancellationToken.None));

        // Assert
        Assert.Equal("Слот записи не найден.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenSlotNotAvailable_ThrowsBusinessRule()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        slot.Book();

        _patients.Setup(r => r.GetByKeycloakIdAsync(patient.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _slots.Setup(r => r.GetByIdWithLockAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(
                new CreateAppointmentDto { SlotId = slot.Id },
                patient.KeycloakId,
                CancellationToken.None));

        // Assert
        Assert.Equal("Слот записи уже занят.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenSlotAlreadyHasAppointment_ThrowsBusinessRule()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        var existing = Appointment.Create(patient.Id, doctor.Id, slot.Id, null);

        _patients.Setup(r => r.GetByKeycloakIdAsync(patient.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _slots.Setup(r => r.GetByIdWithLockAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);
        _appointments.Setup(r => r.GetBySlotIdAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(
                new CreateAppointmentDto { SlotId = slot.Id },
                patient.KeycloakId,
                CancellationToken.None));

        // Assert
        Assert.Equal("На этот слот уже есть запись.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenSlotInPast_ThrowsBusinessRule()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var slot = CreatePastSlot(doctor.Id);

        _patients.Setup(r => r.GetByKeycloakIdAsync(patient.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _slots.Setup(r => r.GetByIdWithLockAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);
        _appointments.Setup(r => r.GetBySlotIdAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(
                new CreateAppointmentDto { SlotId = slot.Id },
                patient.KeycloakId,
                CancellationToken.None));

        // Assert
        Assert.Equal("Нельзя записаться на слот в прошлом.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenDoctorNotFound_ThrowsNotFound()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);

        _patients.Setup(r => r.GetByKeycloakIdAsync(patient.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _slots.Setup(r => r.GetByIdWithLockAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);
        _appointments.Setup(r => r.GetBySlotIdAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);
        _doctors.Setup(r => r.GetByIdAsync(slot.DoctorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.CreateAsync(
                new CreateAppointmentDto { SlotId = slot.Id },
                patient.KeycloakId,
                CancellationToken.None));

        // Assert
        Assert.Equal("Врач не найден.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenDoctorInactive_ThrowsBusinessRule()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        doctor.Deactivate();
        var slot = CreateFutureSlot(doctor.Id);

        _patients.Setup(r => r.GetByKeycloakIdAsync(patient.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _slots.Setup(r => r.GetByIdWithLockAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);
        _appointments.Setup(r => r.GetBySlotIdAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);
        _doctors.Setup(r => r.GetByIdAsync(slot.DoctorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(
                new CreateAppointmentDto { SlotId = slot.Id },
                patient.KeycloakId,
                CancellationToken.None));

        // Assert
        Assert.Equal("Нельзя записаться: врач деактивирован.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_BooksSlotCommitsAndReturnsDto()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        Appointment? added = null;

        _patients.Setup(r => r.GetByKeycloakIdAsync(patient.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _slots.Setup(r => r.GetByIdWithLockAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);
        _appointments.Setup(r => r.GetBySlotIdAsync(slot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);
        _doctors.Setup(r => r.GetByIdAsync(slot.DoctorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _appointments
            .Setup(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .Callback<Appointment, CancellationToken>((a, _) =>
            {
                added = a;
                AttachDetails(a, patient, doctor, slot);
            })
            .Returns(Task.CompletedTask);
        _appointments
            .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => added);

        // Act
        var result = await _sut.CreateAsync(
            new CreateAppointmentDto { SlotId = slot.Id, Reason = "Осмотр" },
            patient.KeycloakId,
            CancellationToken.None);

        // Assert
        Assert.NotNull(added);
        Assert.Equal(added!.Id, result.Id);
        Assert.Equal(SlotStatus.Booked, slot.Status);
        Assert.Equal("Осмотр", result.Reason);
        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _slots.Verify(r => r.UpdateAsync(slot, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Cancel

    [Fact]
    public async Task CancelAsync_WhenAppointmentNotFound_ThrowsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _appointments.Setup(r => r.GetByIdWithLockAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.CancelAsync(id, "any", CancellationToken.None));

        // Assert
        Assert.Equal("Запись не найдена.", ex.Message);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_WhenCallerNotOwner_ThrowsForbidden()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        slot.Book();
        var appointment = CreateAppointmentWithDetails(patient, doctor, slot);
        var stranger = CreatePatient("stranger-kc");

        _appointments.Setup(r => r.GetByIdWithLockAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);
        _patients.Setup(r => r.GetByKeycloakIdAsync(stranger.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stranger);
        _doctors.Setup(r => r.GetByKeycloakIdAsync(stranger.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        // Act
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.CancelAsync(appointment.Id, stranger.KeycloakId, CancellationToken.None));

        // Assert
        Assert.Equal("Нет доступа к этой записи.", ex.Message);
    }

    [Fact]
    public async Task CancelAsync_WhenSlotInPast_ThrowsBusinessRule()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var slot = CreatePastSlot(doctor.Id);
        slot.Book();
        var appointment = CreateAppointmentWithDetails(patient, doctor, slot);

        _appointments.Setup(r => r.GetByIdWithLockAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);
        _patients.Setup(r => r.GetByKeycloakIdAsync(patient.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _slots.Setup(r => r.GetByIdWithLockAsync(appointment.SlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);

        // Act
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CancelAsync(appointment.Id, patient.KeycloakId, CancellationToken.None));

        // Assert
        Assert.Equal("Нельзя отменить запись в прошлом.", ex.Message);
    }

    [Fact]
    public async Task CancelAsync_WhenPatientOwner_CancelsAndFreesSlot()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        slot.Book();
        var appointment = CreateAppointmentWithDetails(patient, doctor, slot);

        _appointments.Setup(r => r.GetByIdWithLockAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);
        _patients.Setup(r => r.GetByKeycloakIdAsync(patient.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _slots.Setup(r => r.GetByIdWithLockAsync(appointment.SlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);

        // Act
        await _sut.CancelAsync(appointment.Id, patient.KeycloakId, CancellationToken.None);

        // Assert
        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Equal(SlotStatus.Available, slot.Status);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Complete

    [Fact]
    public async Task CompleteAsync_WhenDoctorNotFound_ThrowsNotFound()
    {
        // Arrange
        _doctors.Setup(r => r.GetByKeycloakIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.CompleteAsync(Guid.NewGuid(), "missing", CancellationToken.None));

        // Assert
        Assert.Equal("Профиль врача не найден.", ex.Message);
    }

    [Fact]
    public async Task CompleteAsync_WhenNotOwnAppointment_ThrowsForbidden()
    {
        // Arrange
        var patient = CreatePatient();
        var ownerDoctor = CreateDoctor("owner-kc");
        var otherDoctor = CreateDoctor("other-kc");
        var slot = CreateFutureSlot(ownerDoctor.Id);
        slot.Book();
        var appointment = CreateAppointmentWithDetails(patient, ownerDoctor, slot);
        appointment.Confirm();

        _doctors.Setup(r => r.GetByKeycloakIdAsync(otherDoctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherDoctor);
        _appointments.Setup(r => r.GetByIdWithLockAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);

        // Act
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.CompleteAsync(appointment.Id, otherDoctor.KeycloakId, CancellationToken.None));

        // Assert
        Assert.Equal("Врач может завершать только свои записи.", ex.Message);
    }

    [Fact]
    public async Task CompleteAsync_WhenOwnConfirmed_CompletesAndConsumesSlot()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        slot.Book();
        var appointment = CreateAppointmentWithDetails(patient, doctor, slot);
        appointment.Confirm();

        _doctors.Setup(r => r.GetByKeycloakIdAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _appointments.Setup(r => r.GetByIdWithLockAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);
        _slots.Setup(r => r.GetByIdWithLockAsync(appointment.SlotId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);

        // Act
        await _sut.CompleteAsync(appointment.Id, doctor.KeycloakId, CancellationToken.None);

        // Assert
        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
        Assert.Equal(SlotStatus.Consumed, slot.Status);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Confirm ---

    [Fact]
    public async Task ConfirmAsync_WhenDoctorNotFound_ThrowsNotFound()
    {
        // Arrange
        _doctors.Setup(r => r.GetByKeycloakIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.ConfirmAsync(Guid.NewGuid(), "missing", CancellationToken.None));

        // Assert
        Assert.Equal("Профиль врача не найден.", ex.Message);
    }

    [Fact]
    public async Task ConfirmAsync_WhenNotOwnAppointment_ThrowsForbidden()
    {
        // Arrange
        var patient = CreatePatient();
        var ownerDoctor = CreateDoctor("owner-kc");
        var otherDoctor = CreateDoctor("other-kc");
        var slot = CreateFutureSlot(ownerDoctor.Id);
        var appointment = CreateAppointmentWithDetails(patient, ownerDoctor, slot);

        _doctors.Setup(r => r.GetByKeycloakIdAsync(otherDoctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherDoctor);
        _appointments.Setup(r => r.GetByIdWithLockAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);

        // Act
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.ConfirmAsync(appointment.Id, otherDoctor.KeycloakId, CancellationToken.None));

        // Assert
        Assert.Equal("Врач может подтверждать только свои записи.", ex.Message);
    }

    [Fact]
    public async Task ConfirmAsync_WhenOwnCreated_ConfirmsAndCommits()
    {
        // Arrange
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var slot = CreateFutureSlot(doctor.Id);
        var appointment = CreateAppointmentWithDetails(patient, doctor, slot);

        _doctors.Setup(r => r.GetByKeycloakIdAsync(doctor.KeycloakId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);
        _appointments.Setup(r => r.GetByIdWithLockAsync(appointment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);

        // Act
        await _sut.ConfirmAsync(appointment.Id, doctor.KeycloakId, CancellationToken.None);

        // Assert
        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
        _appointments.Verify(r => r.UpdateAsync(appointment, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
