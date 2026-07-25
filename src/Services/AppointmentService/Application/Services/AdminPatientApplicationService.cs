using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Application.Validators;
using AppointmentService.Domain.Entities;
using FluentValidation;
using System.Numerics;

namespace AppointmentService.Application.Services;

public class AdminPatientApplicationService(
    IPatientRepository patientRepository,
    IAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork,
    IValidator<UpdatePatientDto> updatePatientValidator) : IAdminPatientApplicationService
{
    private readonly IPatientRepository _patientRepository = patientRepository;
    private readonly IAppointmentRepository _appointmentRepository = appointmentRepository;    
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IValidator<UpdatePatientDto> _updatePatientValidator = updatePatientValidator;

    public async Task<IReadOnlyList<PatientDto>> GetAllIncludingInactiveAsync(CancellationToken ct)
        => (await _patientRepository.GetAllWithInactiveAsync(ct))
            .Select(MapToDto).ToList();

    public async Task<PatientDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var patient = await _patientRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Пациент {id} не найден.");
        return MapToDto(patient);
    }

    public async Task<PatientDto> UpdateAsync(Guid id, UpdatePatientDto dto, CancellationToken ct)
    {
        var validationResult = await _updatePatientValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var patient = await _patientRepository.GetByIdAsync(id, ct)
                ?? throw new NotFoundException($"Пациент {id} не найден.");

            patient.Update(
                lastName: dto.LastName,
                firstName: dto.FirstName,
                middleName: dto.MiddleName,
                phone: dto.Phone,
                dateOfBirth: dto.DateOfBirth);
            await _patientRepository.UpdateAsync(patient, ct);

            await _unitOfWork.CommitAsync(ct);

            return MapToDto(patient);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct)
    {
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var patient = await _patientRepository.GetByIdAsync(id, ct)
                ?? throw new NotFoundException($"Пациент {id} не найден.");
            
            var hasFuture = await _appointmentRepository.HasConfirmedFutureAppointmentsAsync(patient.Id, DateTime.UtcNow, ct);
            if (hasFuture)
                throw new BusinessRuleException(
                    "Невозможно деактивировать пациента: есть активные будущие записи. " +
                    "Сначала отмените все запланированные приёмы.");

            patient.Deactivate();
            await _patientRepository.UpdateAsync(patient, ct);
            
            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }

        //await _keycloakAdminService.DisableUserAsync(patient.KeycloakId, ct);
    }

    public async Task ActivateAsync(Guid id, CancellationToken ct)
    {
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var patient = await _patientRepository.GetByIdAsync(id, ct)
                ?? throw new NotFoundException($"Пациент {id} не найден.");
            
            patient.Activate();
            await _patientRepository.UpdateAsync(patient, ct);

            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }

        //await _keycloakAdminService.EnableUserAsync(patient.KeycloakId, ct);
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
