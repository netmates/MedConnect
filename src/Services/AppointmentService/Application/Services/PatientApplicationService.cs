using AppointmentService.Application.Common;
using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Helpers;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Entities;
using FluentValidation;
using MedConnect.Shared.Events;

namespace AppointmentService.Application.Services;

public class PatientApplicationService(
    IPatientRepository patientRepository,
    IUnitOfWork unitOfWork,
    IKeycloakAdminService keycloakAdminService,
    IValidator<RegisterPatientDto> registerPatientValidator,
    IValidator<UpdatePatientDto> updatePatientValidator,
    ILogger<PatientApplicationService> logger,
    IIntegrationEventPublisher publisher,
    IHttpContextAccessor httpContextAccessor) : IPatientApplicationService
{
    public async Task<PatientDto> RegisterOrGetAsync(string keycloakId, RegisterPatientDto dto, CancellationToken ct)
    {
        var validationResult = await registerPatientValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // Если профиль уже есть (создан через PatientProvisioningMiddleware при OAuth) — возвращаем его
        var existing = await patientRepository.GetByKeycloakIdAsync(keycloakId, ct);
        if (existing is not null)
            return MapToDto(existing);

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
