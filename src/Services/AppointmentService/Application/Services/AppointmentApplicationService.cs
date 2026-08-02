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

    public async Task<IReadOnlyList<AppointmentDto>> GetByPatientAsync(
        string keycloakId,
        AppointmentStatus? status,
        DateTime? from,
        DateTime? to,
        CancellationToken ct)
    {
        var patient = await _patientRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Пациент не найден.");

        var appointments = await _appointmentRepository.GetByPatientIdAsync(patient.Id, status, from, to, ct);
        return appointments.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetByDoctorAsync(
        string keycloakId,
        AppointmentStatus? status,
        DateTime? from,
        DateTime? to,
        CancellationToken ct)
    {
        var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Врач не найден.");

        var appointments = await _appointmentRepository.GetByDoctorIdAsync(doctor.Id, status, from, to, ct);
        return appointments.Select(MapToDto).ToList();
    }

    public async Task<AppointmentDto> GetByIdAsync(Guid appointmentId, string keycloakId, CancellationToken ct)
    {
        var appointment = await _appointmentRepository.GetByIdWithDetailsAsync(appointmentId, ct)
            ?? throw new NotFoundException("Запись не найдена.");

        var patient = await _patientRepository.GetByKeycloakIdAsync(keycloakId, ct);
        if (patient is not null && appointment.PatientId == patient.Id)
            return MapToDto(appointment);

        var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId, ct);
        if (doctor is not null && appointment.DoctorId == doctor.Id)
            return MapToDto(appointment);

        throw new ForbiddenException("Нет доступа к этой записи.");
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

            var existingAppointment = await _appointmentRepository.GetBySlotIdAsync(slot.Id, ct);
            if (existingAppointment is not null)
                throw new BusinessRuleException("На этот слот уже есть запись.");

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
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        var created = await _appointmentRepository.GetByIdWithDetailsAsync(appointment.Id, ct)
            ?? throw new NotFoundException("Запись не найдена.");
        return MapToDto(created);
    }

    public async Task CancelAsync(Guid appointmentId, string keycloakId, CancellationToken ct)
    {
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var appointment = await _appointmentRepository.GetByIdWithLockAsync(appointmentId, ct)
                ?? throw new NotFoundException("Запись не найдена.");

            var patient = await _patientRepository.GetByKeycloakIdAsync(keycloakId, ct);
            var isPatientOwner = patient is not null && appointment.PatientId == patient.Id;

            var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId, ct);
            var isDoctorOwner = doctor is not null && appointment.DoctorId == doctor.Id;

            if (!isPatientOwner && !isDoctorOwner)
                throw new ForbiddenException("Нет доступа к этой записи.");

            var slot = await _slotRepository.GetByIdWithLockAsync(appointment.SlotId, ct)
                ?? throw new NotFoundException("Слот записи не найден.");

            if (slot.StartTime <= DateTime.UtcNow)
                throw new BusinessRuleException("Нельзя отменить запись в прошлом.");

            appointment.Cancel();
            await _appointmentRepository.UpdateAsync(appointment, ct);

            slot.Free();
            await _slotRepository.UpdateAsync(slot, ct);            

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

            var slot = await _slotRepository.GetByIdWithLockAsync(appointment.SlotId, ct)
                ?? throw new NotFoundException("Слот записи не найден.");

            appointment.Complete();
            await _appointmentRepository.UpdateAsync(appointment, ct);

            slot.Consume();
            await _slotRepository.UpdateAsync(slot, ct);

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
        UpdatedAt = a.UpdatedAt,
        DoctorFullName = FormatFullName(a.Doctor.LastName, a.Doctor.FirstName, a.Doctor.MiddleName),
        PatientFullName = FormatFullName(a.Patient.LastName, a.Patient.FirstName, a.Patient.MiddleName),
        StartTime = a.Slot.StartTime,
        EndTime = a.Slot.EndTime
    };

    private static string FormatFullName(string lastName, string firstName, string? middleName)
        => string.IsNullOrWhiteSpace(middleName)
            ? $"{lastName} {firstName}".Trim()
            : $"{lastName} {firstName} {middleName}".Trim();
}
