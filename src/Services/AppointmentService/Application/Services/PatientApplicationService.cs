using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Helpers;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Entities;
using FluentValidation;

namespace AppointmentService.Application.Services;

public class PatientApplicationService(
    IPatientRepository patientRepository,
    IUnitOfWork unitOfWork,
    IKeycloakAdminService keycloakAdminService,
    IValidator<RegisterPatientDto> registerPatientValidator,
    IValidator<UpdatePatientDto> updatePatientValidator,
    ILogger<PatientApplicationService> logger) : IPatientApplicationService
{
    private readonly IPatientRepository _patientRepository = patientRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IKeycloakAdminService _keycloakAdminService = keycloakAdminService;
    private readonly IValidator<RegisterPatientDto> _registerPatientValidator = registerPatientValidator;
    private readonly IValidator<UpdatePatientDto> _updatePatientValidator = updatePatientValidator;
    private readonly ILogger<PatientApplicationService> _logger = logger;

    public async Task<PatientDto> RegisterOrGetAsync(string keycloakId, RegisterPatientDto dto, CancellationToken ct)
    {
        var validationResult = await _registerPatientValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // Если профиль уже есть (создан через PatientProvisioningMiddleware при OAuth) — возвращаем его
        var existing = await _patientRepository.GetByKeycloakIdAsync(keycloakId, ct);
        if (existing is not null)
            return MapToDto(existing);

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var patient = Patient.Create(
                keycloakId: keycloakId,
                lastName: dto.LastName,
                firstName: dto.FirstName,
                middleName: dto.MiddleName,
                phone: dto.Phone,
                dateOfBirth: dto.DateOfBirth);
            await _patientRepository.AddAsync(patient, ct);

            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "Patient registered: {PatientId}, KeycloakId={KeycloakId}",
                patient.Id, keycloakId);

            return MapToDto(patient);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<PatientDto> GetByKeycloakIdAsync(string keycloakId, CancellationToken ct)
    {
        var patient = await _patientRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль пациента не найден.");

        return MapToDto(patient);
    }

    public async Task<PatientDto> UpdateAsync(string keycloakId, UpdatePatientDto dto, CancellationToken ct)
    {
        var validationResult = await _updatePatientValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var patient = await _patientRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль пациента не найден.");

        var oldFirstName = patient.FirstName;
        var oldLastName = patient.LastName;
        var nameChanged = await KeycloakNameSync.ApplyIfChangedAsync(
            _keycloakAdminService,
            keycloakId,
            oldFirstName,
            oldLastName,
            dto.FirstName,
            dto.LastName,
            ct);

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            patient.Update(
                lastName: dto.LastName,
                firstName: dto.FirstName,
                middleName: dto.MiddleName,
                phone: dto.Phone,
                dateOfBirth: dto.DateOfBirth);
            await _patientRepository.UpdateAsync(patient, ct);

            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "Patient updated: {PatientId}, KeycloakId={KeycloakId}",
                patient.Id, keycloakId);

            return MapToDto(patient);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);

            await KeycloakNameSync.CompensateIfNeededAsync(
                _keycloakAdminService,
                _logger,
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
