using AppointmentService.Application.DTOs.Slot;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using FluentValidation;

namespace AppointmentService.Application.Services;

public class ScheduleApplicationService(
    IScheduleSlotRepository slotRepository,
    IDoctorRepository doctorRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateSlotDto> createSlotValidator,
    IValidator<UpdateSlotDto> updateSlotValidator) : IScheduleApplicationService
{
    private readonly IScheduleSlotRepository _slotRepository = slotRepository;
    private readonly IDoctorRepository _doctorRepository = doctorRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IValidator<CreateSlotDto> _createSlotValidator = createSlotValidator;
    private readonly IValidator<UpdateSlotDto> _updateSlotValidator = updateSlotValidator;

    public async Task<SlotDto> CreateAsync(CreateSlotDto dto, string keycloakId, CancellationToken ct)
    {
        var validationResult = await _createSlotValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль врача не найден.");

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var hasOverlap = await _slotRepository.HasOverlappingSlotAsync(doctor.Id, dto.StartTime, dto.EndTime, null, ct);
            if (hasOverlap)
                throw new BusinessRuleException("Слот пересекается с существующим.");

            var slot = ScheduleSlot.Create(doctor.Id, dto.StartTime, dto.EndTime);
            await _slotRepository.AddAsync(slot, ct);

            await _unitOfWork.CommitAsync(ct);

            return MapToDto(slot);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<SlotDto> UpdateAsync(Guid id, UpdateSlotDto dto, string keycloakId, CancellationToken ct)
    {
        var validationResult = await _updateSlotValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль врача не найден.");        

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var slot = await _slotRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Слот {id} не найден.");

            if (slot.DoctorId != doctor.Id)
                throw new ForbiddenException("Нет доступа к этому слоту.");

            if (slot.Status != SlotStatus.Available)
                throw new BusinessRuleException("Нельзя редактировать слот: он уже забронирован или отменён.");

            var hasOverlap = await _slotRepository.HasOverlappingSlotAsync(doctor.Id, dto.StartTime, dto.EndTime, slot.Id, ct);
            if (hasOverlap)
                throw new BusinessRuleException("Слот пересекается с существующим расписанием.");

            slot.Update(dto.StartTime, dto.EndTime);
            await _slotRepository.UpdateAsync(slot, ct);

            await _unitOfWork.CommitAsync(ct);

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

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var slot = await _slotRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Слот {id} не найден.");

            if (slot.DoctorId != doctor.Id)
                throw new ForbiddenException("Нет доступа к этому слоту.");

            if (slot.Status == SlotStatus.Booked)
                throw new BusinessRuleException("Нельзя удалить забронированный слот.");

            await _slotRepository.DeleteAsync(slot, ct);

            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<IReadOnlyList<SlotDto>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct)
    {
        var slots = await _slotRepository.GetByDoctorIdAsync(doctorId, ct);
        return slots.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<SlotDto>> GetAvailableAsync(Guid doctorId, DateTime date, CancellationToken ct)
    {
        var slots = await _slotRepository.GetAvailableByDoctorIdAsync(doctorId, date, ct);
        return slots.Select(MapToDto).ToList();
    }

    private static SlotDto MapToDto(ScheduleSlot s) => new()
    {
        Id = s.Id,
        DoctorId = s.DoctorId,
        StartTime = s.StartTime,
        EndTime = s.EndTime,
        Status = s.Status.ToString()
    };
}
