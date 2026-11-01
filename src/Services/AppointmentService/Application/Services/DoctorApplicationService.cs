using AppointmentService.Application.Auth;
using AppointmentService.Application.DTOs.Doctor;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Helpers;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Caching;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Entities;
using FluentValidation;
using MedConnect.Shared.Events;
using MedConnect.Shared.Messaging;

namespace AppointmentService.Application.Services;

public sealed class DoctorApplicationService(
    IDoctorRepository doctorRepository,
    IDoctorListCache doctorListCache,
    IDoctorCardCache doctorCardCache,
    IDoctorAvailableSlotCache doctorAvailableSlotCache,
    ISpecializationRepository specializationRepository,
    IAppointmentRepository appointmentRepository,
    IActiveAppointmentCancellation activeAppointmentCancellation,
    IUnitOfWork unitOfWork,
    IKeycloakAdminService keycloakAdminService,
    IValidator<CreateDoctorDto> createDoctorValidator,
    IValidator<UpdateDoctorDto> updateDoctorValidator,
    IValidator<ResetPasswordDto> resetPasswordValidator,
    ILogger<DoctorApplicationService> logger,
    IIntegrationEventPublisher publisher,
    ICorrelationIdAccessor correlationIdAccessor) : IDoctorApplicationService
{
    public async Task<IReadOnlyList<DoctorDto>> GetAllAsync(Guid? specializationId, CancellationToken ct)
    {
        try
        {
            var cached = await doctorListCache.GetAsync(ct);
            if (cached is not null)
                return Filter(cached, specializationId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read doctors cache");
        }

        var doctors = await doctorRepository.GetActiveAsync(ct);
        var items = doctors.Select(MapToCacheEntry).ToList();

        try
        {
            await doctorListCache.SetAsync(items, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write doctors cache");
        }

        return Filter(items, specializationId);
    }

    private static List<DoctorDto> Filter(IReadOnlyList<DoctorListEntry> items, Guid? specializationId)
    {
        if (specializationId is null)
            return items.Select(item => item.Doctor).ToList();

        return items
            .Where(item => item.SpecializationIds.Contains(specializationId.Value))
            .Select(item => item.Doctor)
            .ToList();
    }

    private static DoctorListEntry MapToCacheEntry(Doctor doctor) => new()
    {
        Doctor = MapToDto(doctor),
        SpecializationIds = doctor.DoctorSpecializations
            .Select(link => link.SpecializationId)
            .ToList()
    };

    public async Task<DoctorDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        try
        {
            var cached = await doctorCardCache.GetAsync(id, ct);
            if (cached is not null)
                return cached;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read doctor cache");
        }

        var doctor = await doctorRepository.GetWithSpecializationsAsync(id, ct)
            ?? throw new NotFoundException($"Врач {id} не найден.");

        if (!doctor.IsActive)
            throw new NotFoundException($"Врач {id} не найден.");

        var result = MapToDto(doctor);

        try
        {
            await doctorCardCache.SetAsync(result, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write doctor cache");
        }

        return result;
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
            await DropCacheAsync();

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
            await DropCacheAsync();
            await DropCardCacheAsync(id);

            var updateDoctor = await doctorRepository.GetWithSpecializationsAsync(doctor.Id, ct);

            logger.LogInformation("Doctor updated: {DoctorId}", id);

            if (fullNameChanged)
            {
                var correlationId = correlationIdAccessor.Get();
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

        AppointmentCancellationResult cancellation;
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var activeAppointments = await appointmentRepository.GetActiveByDoctorIdAsync(doctor.Id, ct);
            cancellation = await activeAppointmentCancellation.CancelAsync(activeAppointments, ct);

            doctor.Deactivate();
            await doctorRepository.UpdateAsync(doctor, ct);

            await unitOfWork.CommitAsync(ct);
            await DropCacheAsync();
            await DropCardCacheAsync(id);
            await DropFreedSlotCacheAsync(cancellation.FreedSlots);
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
            id, keycloakId, cancellation.CancelledCount);
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
            await DropCacheAsync();
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

    private async Task DropCacheAsync()
    {
        try
        {
            await doctorListCache.RemoveAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to drop doctors cache");
        }
    }

    private async Task DropCardCacheAsync(Guid doctorId)
    {
        try
        {
            await doctorCardCache.RemoveAsync(doctorId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to drop doctor cache. DoctorId={DoctorId}", doctorId);
        }
    }

    private async Task DropFreedSlotCacheAsync(IReadOnlyList<FreedDoctorSlot> slots)
    {
        foreach (var slot in slots)
        {
            try
            {
                await doctorAvailableSlotCache.RemoveAsync(slot.DoctorId, slot.StartTime, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to drop doctor slots cache. DoctorId={DoctorId}, Day={Day:yyyy-MM-dd}",
                    slot.DoctorId, slot.StartTime);
            }
        }
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
