using AppointmentService.Application.Auth;
using AppointmentService.Application.Common;
using AppointmentService.Application.DTOs.Doctor;
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

public class DoctorApplicationService(
    IDoctorRepository doctorRepository,
    ISpecializationRepository specializationRepository,
    IAppointmentRepository appointmentRepository,
    IScheduleSlotRepository slotRepository,
    IUnitOfWork unitOfWork,
    IKeycloakAdminService keycloakAdminService,
    IValidator<CreateDoctorDto> createDoctorValidator,
    IValidator<UpdateDoctorDto> updateDoctorValidator,
    IValidator<ResetPasswordDto> resetPasswordValidator,
    ILogger<DoctorApplicationService> logger,
    IIntegrationEventPublisher publisher,
    IHttpContextAccessor httpContextAccessor) : IDoctorApplicationService
{
    public async Task<IReadOnlyList<DoctorDto>> GetAllAsync(Guid? specializationId, CancellationToken ct)
    {
        var doctors = specializationId.HasValue
            ? await doctorRepository.GetBySpecializationAsync(specializationId.Value, ct)
            : await doctorRepository.GetActiveAsync(ct);
        return doctors.Select(MapToDto).ToList();
    }

    public async Task<DoctorDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var doctor = await doctorRepository.GetWithSpecializationsAsync(id, ct)
            ?? throw new NotFoundException($"Врач {id} не найден.");

        if (!doctor.IsActive)
            throw new NotFoundException($"Врач {id} не найден.");

        return MapToDto(doctor);
    }

    public async Task<IReadOnlyList<DoctorDto>> GetAllIncludingInactiveAsync(CancellationToken ct)
    {
        var doctors = await doctorRepository.GetAllIncludingInactiveAsync(ct);
        return doctors.Select(MapToDto).ToList();
    }

    public async Task<DoctorDto> CreateAsync(CreateDoctorDto dto, CancellationToken ct)
    {
        var validationResult = await createDoctorValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var keycloakId = await keycloakAdminService.CreateUserAsync(
            email: dto.Email,
            temporaryPassword: dto.TemporaryPassword,
            role: Roles.Doctor,
            firstName: dto.FirstName,
            lastName: dto.LastName,
            ct: ct);

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var doctor = Doctor.Create(
                keycloakId: keycloakId,
                lastName: dto.LastName,
                firstName: dto.FirstName,
                middleName: dto.MiddleName,
                description: dto.Description,
                experienceYears: dto.ExperienceYears);
            await doctorRepository.AddAsync(doctor, ct);

            foreach (var specializationId in dto.SpecializationIds)
            {
                var specialization = await specializationRepository.GetByIdAsync(specializationId, ct)
                    ?? throw new NotFoundException($"Специализация {specializationId} не найдена.");

                await doctorRepository.AddDoctorSpecializationAsync(
                    DoctorSpecialization.Create(doctor.Id, specialization.Id), ct);
            }

            await unitOfWork.CommitAsync(ct);

            var createdDoctor = await doctorRepository.GetWithSpecializationsAsync(doctor.Id, ct);

            logger.LogInformation(
                "Doctor created: {DoctorId}, KeycloakId={KeycloakId}",
                doctor.Id, keycloakId);

            return MapToDto(createdDoctor!);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);

            try
            {
                await keycloakAdminService.DeleteUserAsync(keycloakId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to rollback Keycloak user {KeycloakId} after doctor create failure",
                    keycloakId);
            }

            throw;
        }
    }

    public async Task<DoctorDto> UpdateAsync(Guid id, UpdateDoctorDto dto, CancellationToken ct)
    {
        var validationResult = await updateDoctorValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var doctor = await doctorRepository.GetWithSpecializationsAsync(id, ct)
            ?? throw new NotFoundException($"Врач {id} не найден.");

        var keycloakId = doctor.KeycloakId;
        var oldFirstName = doctor.FirstName;
        var oldLastName = doctor.LastName;
        var oldMiddleName = doctor.MiddleName;
        var oldFullName = FullNameFormatter.Format(oldLastName, oldFirstName, oldMiddleName);
        var newFullName = FullNameFormatter.Format(dto.LastName, dto.FirstName, dto.MiddleName);
        var fullNameChanged = !string.Equals(oldFullName, newFullName, StringComparison.Ordinal);

        var nameChanged = await KeycloakNameSync.ApplyIfChangedAsync(
            keycloakAdminService,
            keycloakId,
            oldFirstName,
            oldLastName,
            dto.FirstName,
            dto.LastName,
            ct);

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            doctor.Update(
                lastName: dto.LastName,
                firstName: dto.FirstName,
                middleName: dto.MiddleName,
                description: dto.Description,
                experienceYears: dto.ExperienceYears);
            await doctorRepository.UpdateAsync(doctor, ct);

            foreach (var specializationId in dto.SpecializationIds)
            {
                _ = await specializationRepository.GetByIdAsync(specializationId, ct)
                    ?? throw new NotFoundException($"Специализация {specializationId} не найдена.");
            }

            var currentIds = doctor.DoctorSpecializations
                .Select(ds => ds.SpecializationId)
                .ToHashSet();
            var desiredIds = dto.SpecializationIds.ToHashSet();

            if (desiredIds.Count == 0)
                throw new BusinessRuleException("Врач должен иметь хотя бы одну специализацию.");

            var toAdd = desiredIds.Except(currentIds);
            var toRemove = currentIds.Except(desiredIds);

            foreach (var specializationId in toAdd)
            {
                await doctorRepository.AddDoctorSpecializationAsync(
                    DoctorSpecialization.Create(doctor.Id, specializationId), ct);
            }

            foreach (var specializationId in toRemove)
            {
                await doctorRepository.RemoveDoctorSpecializationAsync(doctor.Id, specializationId, ct);
            }

            await unitOfWork.CommitAsync(ct);

            var updateDoctor = await doctorRepository.GetWithSpecializationsAsync(doctor.Id, ct);

            logger.LogInformation("Doctor updated: {DoctorId}", id);

            if (fullNameChanged)
            {
                var correlationId = httpContextAccessor.HttpContext?.Items[CorrelationIdKeys.ItemKey] as string;
                try
                {
                    await publisher.PublishAsync(
                        EventTypes.ParticipantNameUpdated,
                        RoutingKeys.ParticipantNameUpdated,
                        new ParticipantNameUpdatedPayload
                        {
                            ParticipantId = doctor.Id,
                            Role = ParticipantRoles.Doctor,
                            FullName = newFullName
                        },
                        correlationId,
                        ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Failed to publish ParticipantNameUpdated. DoctorId={DoctorId}",
                        doctor.Id);
                }
            }

            return MapToDto(updateDoctor!);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);

            await KeycloakNameSync.CompensateIfNeededAsync(
                keycloakAdminService,
                logger,
                nameChanged,
                ex,
                keycloakId,
                oldFirstName,
                oldLastName,
                "DoctorId={DoctorId}, KeycloakId={KeycloakId}",
                id, keycloakId);

            throw;
        }
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct)
    {
        var doctor = await doctorRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Врач {id} не найден.");

        if (!doctor.IsActive)
        {
            logger.LogInformation("Doctor already inactive: {DoctorId}", id);
            return;
        }

        var keycloakId = doctor.KeycloakId;
        await keycloakAdminService.DisableUserAsync(keycloakId, ct);

        int cancelledCount;
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var activeAppointments = await appointmentRepository.GetActiveByDoctorIdAsync(doctor.Id, ct);
            cancelledCount = await CancelActiveAppointmentsAsync(activeAppointments, ct);

            doctor.Deactivate();
            await doctorRepository.UpdateAsync(doctor, ct);

            await unitOfWork.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);

            logger.LogError(
                ex,
                "DB deactivate failed after Keycloak disable. Compensating Keycloak enable. DoctorId={DoctorId}, KeycloakId={KeycloakId}",
                id, keycloakId);

            try
            {
                await keycloakAdminService.EnableUserAsync(keycloakId, CancellationToken.None);
            }
            catch (Exception compensateEx)
            {
                logger.LogError(
                    compensateEx,
                    "Keycloak enable compensation failed for doctor {DoctorId}. Manual fix may be required.",
                    id);
            }

            throw;
        }

        logger.LogInformation(
            "Doctor deactivated: {DoctorId}, KeycloakId={KeycloakId}, CancelledAppointments={Count}",
            id, keycloakId, cancelledCount);
    }

    public async Task ActivateAsync(Guid id, CancellationToken ct)
    {
        var doctor = await doctorRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Врач {id} не найден.");

        var keycloakId = doctor.KeycloakId;
        await keycloakAdminService.EnableUserAsync(keycloakId, ct);

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            doctor.Activate();
            await doctorRepository.UpdateAsync(doctor, ct);

            await unitOfWork.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);

            logger.LogError(
                ex,
                "DB activate failed after Keycloak enable. Compensating Keycloak disable. DoctorId={DoctorId}, KeycloakId={KeycloakId}",
                id, keycloakId);

            try
            {
                await keycloakAdminService.DisableUserAsync(keycloakId, CancellationToken.None);
            }
            catch (Exception compensateEx)
            {
                logger.LogError(
                    compensateEx,
                    "Keycloak disable compensation failed for doctor {DoctorId}. Manual fix may be required.",
                    id);
            }

            throw;
        }

        logger.LogInformation(
            "Doctor activated: {DoctorId}, KeycloakId={KeycloakId}",
            id, keycloakId);
    }

    public async Task ResetPasswordAsync(Guid id, ResetPasswordDto dto, CancellationToken ct)
    {
        var validationResult = await resetPasswordValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var doctor = await doctorRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Врач {id} не найден.");

        await keycloakAdminService.ResetPasswordAsync(doctor.KeycloakId, dto.NewPassword, ct);

        logger.LogInformation(
            "Doctor password reset: {DoctorId}, KeycloakId={KeycloakId}",
            id, doctor.KeycloakId);
    }

    /// <summary>
    /// Отменяет Created/Confirmed. Слот: будущий Booked → Free, прошлый/идущий Booked → Consume.
    /// </summary>
    private async Task<int> CancelActiveAppointmentsAsync(
        IReadOnlyList<Appointment> appointments,
        CancellationToken ct)
    {
        var cancelled = 0;
        var now = DateTime.UtcNow;

        foreach (var item in appointments)
        {
            var appointment = await appointmentRepository.GetByIdWithLockAsync(item.Id, ct);
            if (appointment is null) continue;

            if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
                continue;

            var slot = await slotRepository.GetByIdWithLockAsync(appointment.SlotId, ct)
                ?? throw new NotFoundException("Слот записи не найден.");

            appointment.Cancel();
            await appointmentRepository.UpdateAsync(appointment, ct);
            cancelled++;

            if (slot.Status == SlotStatus.Booked)
            {
                if (slot.StartTime > now)
                    slot.Free();
                else
                    slot.Consume();

                await slotRepository.UpdateAsync(slot, ct);
            }
        }

        return cancelled;
    }

    private static DoctorDto MapToDto(Doctor d) => new()
    {
        Id = d.Id,
        KeycloakId = d.KeycloakId,
        LastName = d.LastName,
        FirstName = d.FirstName,
        MiddleName = d.MiddleName,
        Description = d.Description,
        ExperienceYears = d.ExperienceYears,
        IsActive = d.IsActive,
        Specializations = d.DoctorSpecializations
            .Select(ds => ds.Specialization.Name)
            .ToList()
    };
}
