using AppointmentService.Application.Auth;
using AppointmentService.Application.Common;
using AppointmentService.Application.DTOs.Appointment;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Helpers;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using FluentValidation;
using MedConnect.Shared.Events;

namespace AppointmentService.Application.Services;

public class AppointmentApplicationService(
    IAppointmentRepository appointmentRepository,
    IScheduleSlotRepository slotRepository,
    IPatientRepository patientRepository,
    IDoctorRepository doctorRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateAppointmentDto> createAppointmentValidator,
    ILogger<AppointmentApplicationService> logger,
    IIntegrationEventPublisher publisher,
    IHttpContextAccessor httpContextAccessor) : IAppointmentApplicationService
{
    public async Task<IReadOnlyList<AppointmentDto>> GetByPatientAsync(
        string keycloakId,
        AppointmentStatus? status,
        DateTime? from,
        DateTime? to,
        CancellationToken ct)
    {
        var patient = await patientRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Пациент не найден.");

        var appointments = await appointmentRepository.GetByPatientIdAsync(patient.Id, status, from, to, ct);
        return appointments.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetByDoctorAsync(
        string keycloakId,
        AppointmentStatus? status,
        DateTime? from,
        DateTime? to,
        CancellationToken ct)
    {
        var doctor = await doctorRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Врач не найден.");

        var appointments = await appointmentRepository.GetByDoctorIdAsync(doctor.Id, status, from, to, ct);
        return appointments.Select(MapToDto).ToList();
    }

    public async Task<AppointmentDto> GetByIdAsync(Guid appointmentId, string keycloakId, CancellationToken ct)
    {
        var appointment = await appointmentRepository.GetByIdWithDetailsAsync(appointmentId, ct)
            ?? throw new NotFoundException("Запись не найдена.");

        var patient = await patientRepository.GetByKeycloakIdAsync(keycloakId, ct);
        if (patient is not null && appointment.PatientId == patient.Id)
            return MapToDto(appointment);

        var doctor = await doctorRepository.GetByKeycloakIdAsync(keycloakId, ct);
        if (doctor is not null && appointment.DoctorId == doctor.Id)
            return MapToDto(appointment);

        throw new ForbiddenException("Нет доступа к этой записи.");
    }

    public async Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto, string keycloakId, CancellationToken ct)
    {
        var validationResult = await createAppointmentValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var patient = await patientRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Пациент не найден.");

        if (!patient.IsActive)
            throw new BusinessRuleException("Нельзя записаться: профиль пациента деактивирован.");

        Appointment appointment;
        ScheduleSlot slot;
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            slot = await slotRepository.GetByIdWithLockAsync(dto.SlotId, ct)
                ?? throw new NotFoundException("Слот записи не найден.");

            if (slot.Status != SlotStatus.Available)
                throw new BusinessRuleException("Слот записи уже занят.");

            var existingAppointment = await appointmentRepository.GetBySlotIdAsync(slot.Id, ct);
            if (existingAppointment is not null)
                throw new BusinessRuleException("На этот слот уже есть запись.");

            if (slot.StartTime <= DateTime.UtcNow)
                throw new BusinessRuleException("Нельзя записаться на слот в прошлом.");

            var doctor = await doctorRepository.GetByIdAsync(slot.DoctorId, ct)
                ?? throw new NotFoundException("Врач не найден.");

            if (!doctor.IsActive)
                throw new BusinessRuleException("Нельзя записаться: врач деактивирован.");

            slot.Book();
            await slotRepository.UpdateAsync(slot, ct);

            appointment = Appointment.Create(patient.Id, slot.DoctorId, slot.Id, dto.Reason);
            await appointmentRepository.AddAsync(appointment, ct);

            await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        var created = await appointmentRepository.GetByIdWithDetailsAsync(appointment.Id, ct)
            ?? throw new NotFoundException("Запись не найдена.");

        logger.LogInformation(
            "Appointment created: {AppointmentId}, PatientId={PatientId}, DoctorId={DoctorId}, SlotId={SlotId}",
            created.Id, created.PatientId, created.DoctorId, created.SlotId);

        var correlationId = httpContextAccessor.HttpContext?.Items[CorrelationIdKeys.ItemKey] as string;
        try
        {
            await publisher.PublishAsync(
                EventTypes.AppointmentCreated,
                RoutingKeys.AppointmentCreated,
                new AppointmentCreatedPayload
                {
                    AppointmentId = created.Id,
                    PatientId = created.PatientId,
                    DoctorId = created.DoctorId,
                    SlotId = created.SlotId,
                    StartTime = created.Slot.StartTime,
                    EndTime = created.Slot.EndTime,
                    Reason = created.Reason,
                    PatientKeycloakId = created.Patient.KeycloakId,
                    DoctorKeycloakId = created.Doctor.KeycloakId,
                    PatientName = FullNameFormatter.Format(created.Patient.LastName, created.Patient.FirstName, created.Patient.MiddleName),
                    DoctorName = FullNameFormatter.Format(created.Doctor.LastName, created.Doctor.FirstName, created.Doctor.MiddleName)
                },
                correlationId,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to publish AppointmentCreated. AppointmentId={AppointmentId}",
                created.Id);
        }

        return MapToDto(created);
    }

    public async Task CancelAsync(Guid appointmentId, string keycloakId, CancellationToken ct)
    {
        string cancelledBy;

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var appointment = await appointmentRepository.GetByIdWithLockAsync(appointmentId, ct)
                ?? throw new NotFoundException("Запись не найдена.");

            var patient = await patientRepository.GetByKeycloakIdAsync(keycloakId, ct);
            var isPatientOwner = patient is not null && appointment.PatientId == patient.Id;

            var doctor = await doctorRepository.GetByKeycloakIdAsync(keycloakId, ct);
            var isDoctorOwner = doctor is not null && appointment.DoctorId == doctor.Id;

            if (!isPatientOwner && !isDoctorOwner)
                throw new ForbiddenException("Нет доступа к этой записи.");

            cancelledBy = isPatientOwner ? Roles.Patient : Roles.Doctor;

            var slot = await slotRepository.GetByIdWithLockAsync(appointment.SlotId, ct)
                ?? throw new NotFoundException("Слот записи не найден.");

            if (slot.StartTime <= DateTime.UtcNow)
                throw new BusinessRuleException("Нельзя отменить запись в прошлом.");

            appointment.Cancel();
            await appointmentRepository.UpdateAsync(appointment, ct);

            slot.Free();
            await slotRepository.UpdateAsync(slot, ct);

            await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        logger.LogInformation(
            "Appointment cancelled: {AppointmentId}, CancelledBy={CancelledBy}",
            appointmentId, cancelledBy);
    }

    public async Task CompleteAsync(Guid appointmentId, string keycloakId, CancellationToken ct)
    {
        var doctor = await doctorRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль врача не найден.");

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var appointment = await appointmentRepository.GetByIdWithLockAsync(appointmentId, ct)
                ?? throw new NotFoundException("Запись не найдена.");

            if (appointment.DoctorId != doctor.Id)
                throw new ForbiddenException("Врач может завершать только свои записи.");

            var slot = await slotRepository.GetByIdWithLockAsync(appointment.SlotId, ct)
                ?? throw new NotFoundException("Слот записи не найден.");

            appointment.Complete();
            await appointmentRepository.UpdateAsync(appointment, ct);

            slot.Consume();
            await slotRepository.UpdateAsync(slot, ct);

            await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        logger.LogInformation(
            "Appointment completed: {AppointmentId}, DoctorId={DoctorId}",
            appointmentId, doctor.Id);
    }

    public async Task ConfirmAsync(Guid appointmentId, string keycloakId, CancellationToken ct)
    {
        var doctor = await doctorRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль врача не найден.");

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var appointment = await appointmentRepository.GetByIdWithLockAsync(appointmentId, ct)
                ?? throw new NotFoundException("Запись не найдена.");

            if (appointment.DoctorId != doctor.Id)
                throw new ForbiddenException("Врач может подтверждать только свои записи.");

            appointment.Confirm();
            await appointmentRepository.UpdateAsync(appointment, ct);

            await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        logger.LogInformation(
            "Appointment confirmed: {AppointmentId}, DoctorId={DoctorId}",
            appointmentId, doctor.Id);
    }

    public async Task<ValidateAppointmentAccessResult> ValidateAccessAsync(Guid appointmentId, string keycloakId, CancellationToken ct)
    {
        var appointment = await appointmentRepository.GetByIdWithDetailsAsync(appointmentId, ct);
        if (appointment is null)
        {
            logger.LogWarning(
                "Appointment access denied: not found. AppointmentId={AppointmentId}, KeycloakId={KeycloakId}",
                appointmentId, keycloakId);
            return ValidateAppointmentAccessResult.Deny(AppointmentAccessDenial.NotFound);
        }

        var isPatient = appointment.Patient.KeycloakId == keycloakId;
        var isDoctor = appointment.Doctor.KeycloakId == keycloakId;
        if (!isPatient && !isDoctor)
        {
            logger.LogWarning(
                "Appointment access denied: forbidden. AppointmentId={AppointmentId}, KeycloakId={KeycloakId}",
                appointmentId, keycloakId);
            return ValidateAppointmentAccessResult.Deny(AppointmentAccessDenial.Forbidden);
        }

        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
        {
            logger.LogWarning(
                "Appointment access denied: closed. AppointmentId={AppointmentId}, Status={Status}, KeycloakId={KeycloakId}",
                appointmentId, appointment.Status, keycloakId);
            return ValidateAppointmentAccessResult.Deny(AppointmentAccessDenial.Closed);
        }

        logger.LogDebug(
            "Appointment access allowed. AppointmentId={AppointmentId}, KeycloakId={KeycloakId}",
            appointmentId, keycloakId);
        return ValidateAppointmentAccessResult.Allow(
            appointment.Id,
            appointment.PatientId,
            appointment.DoctorId,
            appointment.Patient.KeycloakId,
            appointment.Doctor.KeycloakId,
            FullNameFormatter.Format(appointment.Patient.LastName, appointment.Patient.FirstName, appointment.Patient.MiddleName),
            FullNameFormatter.Format(appointment.Doctor.LastName, appointment.Doctor.FirstName, appointment.Doctor.MiddleName));
    }

    private static AppointmentDto MapToDto(Appointment a) => new()
    {
        Id = a.Id,
        PatientId = a.PatientId,
        DoctorId = a.DoctorId,
        SlotId = a.SlotId,
        Reason = a.Reason,
        Status = a.Status.ToString(),
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt,
        DoctorFullName = FullNameFormatter.Format(a.Doctor.LastName, a.Doctor.FirstName, a.Doctor.MiddleName),
        PatientFullName = FullNameFormatter.Format(a.Patient.LastName, a.Patient.FirstName, a.Patient.MiddleName),
        StartTime = a.Slot.StartTime,
        EndTime = a.Slot.EndTime
    };
}
