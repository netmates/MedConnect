using AppointmentService.Application.Auth;
using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Helpers;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Entities;
using FluentValidation;
using MedConnect.Shared.Events;
using MedConnect.Shared.Messaging;

namespace AppointmentService.Application.Services;

public sealed class PatientApplicationService(
    IPatientRepository patientRepository,
    IUnitOfWork unitOfWork,
    IKeycloakAdminService keycloakAdminService,
    IValidator<SignUpPatientDto> signUpPatientValidator,
    IValidator<UpdatePatientDto> updatePatientValidator,
    ILogger<PatientApplicationService> logger,
    IIntegrationEventPublisher publisher,
    ICorrelationIdAccessor correlationIdAccessor) : IPatientApplicationService
{
    public async Task<PatientDto> SignUpAsync(SignUpPatientDto dto, CancellationToken ct)
    {
        var validationResult = await signUpPatientValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var keycloakId = await keycloakAdminService.CreateUserAsync(
            email: dto.Email.Trim(),
            temporaryPassword: dto.Password,
            role: Roles.Patient,
            firstName: dto.FirstName,
            lastName: dto.LastName,
            temporary: false,
            ct: ct);

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var patient = Patient.Create(
                keycloakId: keycloakId,
                lastName: dto.LastName,
                firstName: dto.FirstName,
                middleName: dto.MiddleName,
                phone: dto.Phone,
                dateOfBirth: dto.DateOfBirth);
            await patientRepository.AddAsync(patient, ct);

            await unitOfWork.CommitAsync(ct);

            logger.LogInformation(
                "Patient registered: {PatientId}, KeycloakId={KeycloakId}",
                patient.Id, keycloakId);

            return MapToDto(patient);
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
                    "Failed to rollback Keycloak user {KeycloakId} after patient sign-up failure",
                    keycloakId);
            }

            throw;
        }
    }

    public async Task<PatientDto> GetByKeycloakIdAsync(string keycloakId, CancellationToken ct)
    {
        var patient = await patientRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль пациента не найден.");

        return MapToDto(patient);
    }

    public async Task<PatientDto> UpdateAsync(string keycloakId, UpdatePatientDto dto, CancellationToken ct)
    {
        var validationResult = await updatePatientValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var patient = await patientRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль пациента не найден.");

        if (!patient.IsActive)
            throw new BusinessRuleException("Профиль пациента деактивирован.");

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

            logger.LogInformation(
                "Patient updated: {PatientId}, KeycloakId={KeycloakId}",
                patient.Id, keycloakId);

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
                patient.Id, keycloakId);

            throw;
        }
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
