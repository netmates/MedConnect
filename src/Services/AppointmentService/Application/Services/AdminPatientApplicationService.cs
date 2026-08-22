using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using AppointmentService.Infrastructure.Repositories;
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
    private readonly IPatientRepository _patientRepository = patientRepository;
    private readonly IAppointmentRepository _appointmentRepository = appointmentRepository;
    private readonly IScheduleSlotRepository _slotRepository = slotRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IKeycloakAdminService _keycloakAdminService = keycloakAdminService;
    private readonly IValidator<UpdatePatientDto> _updatePatientValidator = updatePatientValidator;
    private readonly ILogger<AdminPatientApplicationService> _logger = logger;

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

            _logger.LogInformation("Patient updated by admin: {PatientId}", id);

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
        int cancelledCount;

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var patient = await _patientRepository.GetByIdAsync(id, ct)
                ?? throw new NotFoundException($"Пациент {id} не найден.");

            var active = await _appointmentRepository.GetActiveFutureByPatientIdAsync(patient.Id, DateTime.UtcNow, ct);

            cancelledCount = await CancelActiveFutureAppointmentsAsync(active, ct);

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

        try
        {
            await _keycloakAdminService.DisableUserAsync(keycloakId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Keycloak disable failed after DB deactivate. Compensating IsActive. PatientId={PatientId}, KeycloakId={KeycloakId}",
                id, keycloakId);

            await CompensatePatientActiveAsync(id, activate: true, ct);
            throw;
        }

        _logger.LogInformation(
            "Patient deactivated: {PatientId}, KeycloakId={KeycloakId}, CancelledAppointments={Count}",
            id, keycloakId, cancelledCount);
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

        try
        {
            await _keycloakAdminService.EnableUserAsync(keycloakId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Keycloak enable failed after DB activate. Compensating IsActive. PatientId={PatientId}, KeycloakId={KeycloakId}",
                id, keycloakId);

            await CompensatePatientActiveAsync(id, activate: false, ct);
            throw;
        }

        _logger.LogInformation(
            "Patient activated: {PatientId}, KeycloakId={KeycloakId}",
            id, keycloakId);
    }

    /// <summary>
    /// Откат IsActive в Postgres, если Keycloak не подтвердил enable/disable.
    /// Отменённые при deactivate записи не восстанавливаются.
    /// </summary>
    private async Task CompensatePatientActiveAsync(Guid id, bool activate, CancellationToken ct)
    {
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var patient = await _patientRepository.GetByIdAsync(id, ct)
                ?? throw new NotFoundException($"Пациент {id} не найден при компенсации.");

            if (activate)
                patient.Activate();
            else
                patient.Deactivate();

            await _patientRepository.UpdateAsync(patient, ct);
            await _unitOfWork.CommitAsync(ct);
        }
        catch (Exception compensateEx)
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);

            _logger.LogError(
                compensateEx,
                "Compensation failed for patient {PatientId}. Manual fix may be required.",
                id);
            throw;
        }
    }

    private async Task<int> CancelActiveFutureAppointmentsAsync(
        IReadOnlyList<Appointment> appointments,
        CancellationToken ct)
    {
        var cancelled = 0;

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
            cancelled++;

            if (slot.Status == SlotStatus.Booked)
            {
                slot.Free();
                await _slotRepository.UpdateAsync(slot, ct);
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
