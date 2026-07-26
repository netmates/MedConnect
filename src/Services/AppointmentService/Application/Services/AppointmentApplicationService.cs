using AppointmentService.Application.DTOs.Appointment;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using FluentValidation;

namespace AppointmentService.Application.Services;

public class AppointmentApplicationService(
    IAppointmentRepository appointmentRepository,
    IScheduleSlotRepository slotRepository,
    IPatientRepository patientRepository,
    IDoctorRepository doctorRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateAppointmentDto> createAppointmentValidator) : IAppointmentApplicationService
{
    private readonly IAppointmentRepository _appointmentRepository = appointmentRepository;
    private readonly IScheduleSlotRepository _slotRepository = slotRepository;
    private readonly IPatientRepository _patientRepository = patientRepository;    
    private readonly IDoctorRepository _doctorRepository = doctorRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IValidator<CreateAppointmentDto> _createAppointmentValidator = createAppointmentValidator;
    
    public async Task<IReadOnlyList<AppointmentDto>> GetByPatientAsync(string keycloakId, CancellationToken ct)
    {
        var patient = await _patientRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Пациент не найден.");

        var appointments = await _appointmentRepository.GetByPatientIdAsync(patient.Id, ct);
        return appointments.Select(MapToDto).ToList();
    }
    
    public async Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto, string keycloakId, CancellationToken ct)
    {
        var validationResult = await _createAppointmentValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var patient = await _patientRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Пациент не найден.");

        if (!patient.IsActive)
            throw new BusinessRuleException("Нельзя записаться: профиль пациента деактивирован.");

        Appointment appointment;
        ScheduleSlot slot;
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            slot = await _slotRepository.GetByIdWithLockAsync(dto.SlotId, ct)
                ?? throw new NotFoundException("Слот записи не найден.");

            if (slot.Status != SlotStatus.Available)
                throw new BusinessRuleException("Слот записи уже занят.");

            if (slot.StartTime <= DateTime.UtcNow)
                throw new BusinessRuleException("Нельзя записаться на слот в прошлом.");

            var doctor = await _doctorRepository.GetByIdAsync(slot.DoctorId, ct)
                ?? throw new NotFoundException("Врач не найден.");
            
            if (!doctor.IsActive)
                throw new BusinessRuleException("Нельзя записаться: врач деактивирован.");

            slot.Book();
            await _slotRepository.UpdateAsync(slot, ct);

            appointment = Appointment.Create(patient.Id, slot.DoctorId, slot.Id, dto.Reason);
            await _appointmentRepository.AddAsync(appointment, ct);
            
            await _unitOfWork.CommitAsync(ct);

            return MapToDto(appointment);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task CancelAsync(Guid appointmentId, string keycloakId, CancellationToken ct)
    {
        var patient = await _patientRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Пациент не найден.");

        Appointment appointment;
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            appointment = await _appointmentRepository.GetByIdWithLockAsync(appointmentId, ct)
                ?? throw new NotFoundException("Запись не найдена.");

            if (appointment.PatientId != patient.Id)
                throw new ForbiddenException("Нет доступа к этой записи.");

            var slot = await _slotRepository.GetByIdWithLockAsync(appointment.SlotId, ct)
                ?? throw new NotFoundException("Слот записи не найден.");

            slot.Free();
            await _slotRepository.UpdateAsync(slot, ct);

            appointment.Cancel();
            await _appointmentRepository.UpdateAsync(appointment, ct);

            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
    
    public async Task CompleteAsync(Guid appointmentId, string keycloakId, CancellationToken ct)
    {
        var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль врача не найден.");

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var appointment = await _appointmentRepository.GetByIdWithLockAsync(appointmentId, ct)
                ?? throw new NotFoundException("Запись не найдена.");

            if (appointment.DoctorId != doctor.Id)
                throw new ForbiddenException("Врач может завершать только свои записи.");

            appointment.Complete();
            await _appointmentRepository.UpdateAsync(appointment, ct);

            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
    
    public async Task ConfirmAsync(Guid appointmentId, string keycloakId, CancellationToken ct)
    {
        var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль врача не найден.");

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var appointment = await _appointmentRepository.GetByIdWithLockAsync(appointmentId, ct)
                ?? throw new NotFoundException("Запись не найдена.");

            if (appointment.DoctorId != doctor.Id)
                throw new ForbiddenException("Врач может подтверждать только свои записи.");

            appointment.Confirm();
            await _appointmentRepository.UpdateAsync(appointment, ct);

            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static AppointmentDto MapToDto(Appointment a) => new()
    {
        Id = a.Id,
        PatientId = a.PatientId,
        DoctorId = a.DoctorId,
        SlotId = a.SlotId,
        Reason = a.Reason,
        Status = a.Status.ToString(),
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };
}
