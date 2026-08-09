using AppointmentService.Application.DTOs.ScheduleSlot;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using FluentValidation;

namespace AppointmentService.Application.Services;

public class ScheduleSlotApplicationService(
    IScheduleSlotRepository slotRepository,
    IDoctorRepository doctorRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateScheduleSlotDto> createSlotValidator,
    IValidator<UpdateScheduleSlotDto> updateSlotValidator,
    ILogger<ScheduleSlotApplicationService> logger) : IScheduleSlotApplicationService
{
    private readonly IScheduleSlotRepository _slotRepository = slotRepository;
    private readonly IDoctorRepository _doctorRepository = doctorRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IValidator<CreateScheduleSlotDto> _createSlotValidator = createSlotValidator;
    private readonly IValidator<UpdateScheduleSlotDto> _updateSlotValidator = updateSlotValidator;
    private readonly ILogger<ScheduleSlotApplicationService> _logger = logger;

    public async Task<ScheduleSlotDto> CreateAsync(CreateScheduleSlotDto dto, string keycloakId, CancellationToken ct)
    {
        var validationResult = await _createSlotValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль врача не найден.");

        if (!doctor.IsActive)
            throw new BusinessRuleException("Нельзя управлять расписанием: профиль врача деактивирован.");

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var hasOverlap = await _slotRepository.HasOverlappingSlotAsync(doctor.Id, dto.StartTime, dto.EndTime, null, ct);
            if (hasOverlap)
                throw new BusinessRuleException("Слот пересекается с существующим.");

            var slot = ScheduleSlot.Create(doctor.Id, dto.StartTime, dto.EndTime);
            await _slotRepository.AddAsync(slot, ct);

            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "Schedule slot created: {SlotId}, DoctorId={DoctorId}, Start={StartTime:o}, End={EndTime:o}",
                slot.Id, doctor.Id, slot.StartTime, slot.EndTime);

            return MapToDto(slot);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<ScheduleSlotDto> UpdateAsync(Guid id, UpdateScheduleSlotDto dto, string keycloakId, CancellationToken ct)
    {
        var validationResult = await _updateSlotValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль врача не найден.");

        if (!doctor.IsActive)
            throw new BusinessRuleException("Нельзя управлять расписанием: профиль врача деактивирован.");

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var slot = await _slotRepository.GetByIdWithLockAsync(id, ct)
                ?? throw new NotFoundException($"Слот {id} не найден.");

            if (slot.DoctorId != doctor.Id)
                throw new ForbiddenException("Нет доступа к этому слоту.");

            if (slot.Status != SlotStatus.Available)
                throw new BusinessRuleException("Нельзя редактировать слот: он уже забронирован.");

            var hasOverlap = await _slotRepository.HasOverlappingSlotAsync(doctor.Id, dto.StartTime, dto.EndTime, slot.Id, ct);
            if (hasOverlap)
                throw new BusinessRuleException("Слот пересекается с существующим расписанием.");

            slot.Update(dto.StartTime, dto.EndTime);
            await _slotRepository.UpdateAsync(slot, ct);

            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation(
                "Schedule slot updated: {SlotId}, DoctorId={DoctorId}, Start={StartTime:o}, End={EndTime:o}",
                slot.Id, doctor.Id, slot.StartTime, slot.EndTime);

            return MapToDto(slot);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, string keycloakId, CancellationToken ct)
    {
        var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль врача не найден.");

        if (!doctor.IsActive)
            throw new BusinessRuleException("Нельзя управлять расписанием: профиль врача деактивирован.");

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var slot = await _slotRepository.GetByIdWithLockAsync(id, ct)
                ?? throw new NotFoundException($"Слот {id} не найден.");

            if (slot.DoctorId != doctor.Id)
                throw new ForbiddenException("Нет доступа к этому слоту.");

            if (slot.Status != SlotStatus.Available)
                throw new BusinessRuleException("Удалить можно только свободный слот.");

            await _slotRepository.DeleteAsync(slot, ct);

            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        _logger.LogInformation(
            "Schedule slot deleted: {SlotId}, DoctorId={DoctorId}",
            id, doctor.Id);
    }

    public async Task<IReadOnlyList<ScheduleSlotDto>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct)
    {
        _ = await _doctorRepository.GetByIdAsync(doctorId, ct)
            ?? throw new NotFoundException("Врач не найден.");

        var slots = await _slotRepository.GetByDoctorIdAsync(doctorId, ct);
        return slots.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ScheduleSlotDto>> GetAvailableAsync(Guid doctorId, DateTime date, CancellationToken ct)
    {
        _ = await _doctorRepository.GetByIdAsync(doctorId, ct)
            ?? throw new NotFoundException("Врач не найден.");

        var slots = await _slotRepository.GetAvailableByDoctorIdAsync(doctorId, date, ct);
        return slots.Select(MapToDto).ToList();
    }

    private static ScheduleSlotDto MapToDto(ScheduleSlot s) => new()
    {
        Id = s.Id,
        DoctorId = s.DoctorId,
        StartTime = s.StartTime,
        EndTime = s.EndTime,
        Status = s.Status.ToString(),
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };
}
