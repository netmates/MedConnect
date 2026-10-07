using MedConnect.Shared.Http;
using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Helpers;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Entities;
using FluentValidation;
using MedConnect.Shared.Messaging;
using MedConnect.Shared.Events;

namespace AppointmentService.Application.Services;

public sealed class AdminPatientApplicationService(
    IPatientRepository patientRepository,
    IAppointmentRepository appointmentRepository,
    IActiveAppointmentCancellation activeAppointmentCancellation,
    IUnitOfWork unitOfWork,
    IKeycloakAdminService keycloakAdminService,
    IValidator<UpdatePatientDto> updatePatientValidator,
    ILogger<AdminPatientApplicationService> logger,
    IIntegrationEventPublisher publisher,
    IHttpContextAccessor httpContextAccessor) : IAdminPatientApplicationService
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
        var oldMiddleName = patient.MiddleName;
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
            patient.Update(
                lastName: dto.LastName,
                firstName: dto.FirstName,
                middleName: dto.MiddleName,
                phone: dto.Phone,
                dateOfBirth: dto.DateOfBirth);
            await patientRepository.UpdateAsync(patient, ct);

            await unitOfWork.CommitAsync(ct);

            logger.LogInformation("Patient updated by admin: {PatientId}", id);

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
                            ParticipantId = patient.Id,
                            Role = ParticipantRoles.Patient,
                            FullName = newFullName
                        },
                        correlationId,
                        ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Failed to publish ParticipantNameUpdated. PatientId={PatientId}",
                        patient.Id);
                }
            }

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
            cancelledCount = await activeAppointmentCancellation.CancelAsync(activeAppointments, ct);

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
