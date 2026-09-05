using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Helpers;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using FluentValidation;

namespace AppointmentService.Application.Services;

public class AdminPatientApplicationService(
    IPatientRepository patientRepository,
    IAppointmentRepository appointmentRepository,
    IScheduleSlotRepository slotRepository,
    IUnitOfWork unitOfWork,
    IKeycloakAdminService keycloakAdminService,
    IValidator<UpdatePatientDto> updatePatientValidator,
    ILogger<AdminPatientApplicationService> logger) : IAdminPatientApplicationService
{
    public async Task<IReadOnlyList<PatientDto>> GetAllIncludingInactiveAsync(CancellationToken ct)
        => (await patientRepository.GetAllIncludingInactiveAsync(ct))
            .Select(MapToDto).ToList();

    public async Task<PatientDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var patient = await patientRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Пациент {id} не найден.");
        return MapToDto(patient);
    }

    public async Task<PatientDto> UpdateAsync(Guid id, UpdatePatientDto dto, CancellationToken ct)
    {
        var validationResult = await updatePatientValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var patient = await patientRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Пациент {id} не найден.");

        var keycloakId = patient.KeycloakId;
        var oldFirstName = patient.FirstName;
        var oldLastName = patient.LastName;
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
            patient.Update(
                lastName: dto.LastName,
                firstName: dto.FirstName,
                middleName: dto.MiddleName,
                phone: dto.Phone,
                dateOfBirth: dto.DateOfBirth);
            await patientRepository.UpdateAsync(patient, ct);

            await unitOfWork.CommitAsync(ct);

            logger.LogInformation("Patient updated by admin: {PatientId}", id);

            return MapToDto(patient);
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
                "PatientId={PatientId}, KeycloakId={KeycloakId}",
                id, keycloakId);

            throw;
        }
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct)
    {
        var patient = await patientRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Пациент {id} не найден.");

        if (!patient.IsActive)
        {
            logger.LogInformation("Patient already inactive: {PatientId}", id);
            return;
        }

        var keycloakId = patient.KeycloakId;
        await keycloakAdminService.DisableUserAsync(keycloakId, ct);

        int cancelledCount;
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var activeAppointments = await appointmentRepository.GetActiveByPatientIdAsync(patient.Id, ct);
            cancelledCount = await CancelActiveAppointmentsAsync(activeAppointments, ct);

            patient.Deactivate();
            await patientRepository.UpdateAsync(patient, ct);

            await unitOfWork.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);

            logger.LogError(
                ex,
                "DB deactivate failed after Keycloak disable. Compensating Keycloak enable. PatientId={PatientId}, KeycloakId={KeycloakId}",
                id, keycloakId);

            try
            {
                await keycloakAdminService.EnableUserAsync(keycloakId, CancellationToken.None);
            }
            catch (Exception compensateEx)
            {
                logger.LogError(
                    compensateEx,
                    "Keycloak enable compensation failed for patient {PatientId}. Manual fix may be required.",
                    id);
            }

            throw;
        }

        logger.LogInformation(
            "Patient deactivated: {PatientId}, KeycloakId={KeycloakId}, CancelledAppointments={Count}",
            id, keycloakId, cancelledCount);
    }

    public async Task ActivateAsync(Guid id, CancellationToken ct)
    {
        var patient = await patientRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Пациент {id} не найден.");

        var keycloakId = patient.KeycloakId;
        await keycloakAdminService.EnableUserAsync(keycloakId, ct);

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            patient.Activate();
            await patientRepository.UpdateAsync(patient, ct);

            await unitOfWork.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);

            logger.LogError(
                ex,
                "DB activate failed after Keycloak enable. Compensating Keycloak disable. PatientId={PatientId}, KeycloakId={KeycloakId}",
                id, keycloakId);

            try
            {
                await keycloakAdminService.DisableUserAsync(keycloakId, CancellationToken.None);
            }
            catch (Exception compensateEx)
            {
                logger.LogError(
                    compensateEx,
                    "Keycloak disable compensation failed for patient {PatientId}. Manual fix may be required.",
                    id);
            }

            throw;
        }

        logger.LogInformation(
            "Patient activated: {PatientId}, KeycloakId={KeycloakId}",
            id, keycloakId);
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

    private static PatientDto MapToDto(Patient p) => new()
    {
        Id = p.Id,
        KeycloakId = p.KeycloakId,
        LastName = p.LastName,
        FirstName = p.FirstName,
        MiddleName = p.MiddleName,
        Phone = p.Phone,
        DateOfBirth = p.DateOfBirth,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt
    };
}
