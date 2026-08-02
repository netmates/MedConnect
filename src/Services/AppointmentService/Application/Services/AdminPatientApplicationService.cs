using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Application.Exceptions;
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
    IValidator<UpdatePatientDto> updatePatientValidator) : IAdminPatientApplicationService
{
    private readonly IPatientRepository _patientRepository = patientRepository;
    private readonly IAppointmentRepository _appointmentRepository = appointmentRepository;
    private readonly IScheduleSlotRepository _slotRepository = slotRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IKeycloakAdminService _keycloakAdminService = keycloakAdminService;
    private readonly IValidator<UpdatePatientDto> _updatePatientValidator = updatePatientValidator;

    public async Task<IReadOnlyList<PatientDto>> GetAllIncludingInactiveAsync(CancellationToken ct)
        => (await _patientRepository.GetAllIncludingInactiveAsync(ct))
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
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct)
    {
        string keycloakId;

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var patient = await _patientRepository.GetByIdAsync(id, ct)
                ?? throw new NotFoundException($"Пациент {id} не найден.");

            await CancelActiveFutureAppointmentsAsync(
                await _appointmentRepository.GetActiveFutureByPatientIdAsync(patient.Id, DateTime.UtcNow, ct),
                ct);

            keycloakId = patient.KeycloakId;

            patient.Deactivate();
            await _patientRepository.UpdateAsync(patient, ct);
            
            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        await _keycloakAdminService.DisableUserAsync(keycloakId, ct);
    }

    public async Task ActivateAsync(Guid id, CancellationToken ct)
    {
        string keycloakId;

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var patient = await _patientRepository.GetByIdAsync(id, ct)
                ?? throw new NotFoundException($"Пациент {id} не найден.");

            keycloakId = patient.KeycloakId;

            patient.Activate();
            await _patientRepository.UpdateAsync(patient, ct);

            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        await _keycloakAdminService.EnableUserAsync(keycloakId, ct);
    }

    private async Task CancelActiveFutureAppointmentsAsync(
        IReadOnlyList<Appointment> appointments,
        CancellationToken ct)
    {
        foreach (var item in appointments)
        {
            var appointment = await _appointmentRepository.GetByIdWithLockAsync(item.Id, ct);
            if (appointment is null) continue;

            if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
                continue;

            var slot = await _slotRepository.GetByIdWithLockAsync(appointment.SlotId, ct)
                ?? throw new NotFoundException("Слот записи не найден.");

            appointment.Cancel();
            await _appointmentRepository.UpdateAsync(appointment, ct);

            if (slot.Status == SlotStatus.Booked)
            {
                slot.Free();
                await _slotRepository.UpdateAsync(slot, ct);
            }
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
