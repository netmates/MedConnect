using AppointmentService.Application.DTOs.Appointment;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
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
    IValidator<CreateAppointmentDto> createAppointmentValidator)
{
    private readonly IAppointmentRepository _appointmentRepository = appointmentRepository;
    private readonly IScheduleSlotRepository _slotRepository = slotRepository;
    private readonly IPatientRepository _patientRepository = patientRepository;    
    private readonly IDoctorRepository _doctorRepository = doctorRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IValidator<CreateAppointmentDto> _createAppointmentValidator = createAppointmentValidator;

    /// <summary>
    /// Возвращает пациенту список его записей
    /// </summary>
    public async Task<IReadOnlyList<AppointmentDto>> GetByPatientAsync(string keycloakId, CancellationToken ct)
    {
        var patient = await _patientRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Пациент не найден.");

        var appointments = await _appointmentRepository.GetByPatientIdAsync(patient.Id, ct);
        return appointments.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Создает запись
    /// </summary>
    public async Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto, string keycloakId, CancellationToken ct)
    {
        var validationResult = await _createAppointmentValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var patient = await _patientRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Пациент не найден.");

        Appointment appointment;
        ScheduleSlot slot;
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            slot = await _slotRepository.GetByIdWithLockAsync(dto.SlotId, ct)
            ?? throw new NotFoundException("Слот записи не найден.");

            if (slot.Status != SlotStatus.Available)
              throw new BusinessRuleException("Слот записи уже занят.");
        
            slot.Book();
            await _slotRepository.UpdateAsync(slot, ct);

            appointment = Appointment.Create(patient.Id, slot.DoctorId, slot.Id, dto.Reason);
            await _appointmentRepository.AddAsync(appointment, ct);

            // Фиксируем оба изменения одной транзакцией
            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        return MapToDto(appointment);
    }

    /// <summary>
    /// Отменяет запись
    /// </summary>
    public async Task CancelAsync(Guid appointmentId, string keycloakId, CancellationToken ct)
    {
        var patient = await _patientRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Пациент не найден.");

        Appointment appointment;
        // Отменяем Appointment + освобождаем Slot в одной транзакции
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

    /// <summary>
    /// Отметить запись как завершённую
    /// </summary>
    public async Task CompleteAsync(Guid appointmentId, Guid doctorId, CancellationToken ct)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId, ct)
            ?? throw new NotFoundException("Запись не найдена.");

        if (appointment.DoctorId != doctorId)
            throw new ForbiddenException("Врач может завершать только свои записи.");

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            appointment.Complete();
            await _appointmentRepository.UpdateAsync(appointment, ct);

            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>
    /// Подтверждение записи
    /// </summary>
    public async Task ConfirmAsync(Guid appointmentId, Guid doctorId, CancellationToken ct)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId, ct)
            ?? throw new NotFoundException("Запись не найдена.");

        if (appointment.DoctorId != doctorId)
            throw new ForbiddenException("Врач может подтверждать только свои записи.");
        
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {   
            appointment.Confirm();
            await _appointmentRepository.UpdateAsync(appointment, ct);

            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
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
