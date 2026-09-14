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
    IAppointmentRepository appointmentRepository,
    IScheduleSlotRepository slotRepository,
    IDoctorRepository doctorRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateScheduleSlotDto> createSlotValidator,
    IValidator<UpdateScheduleSlotDto> updateSlotValidator,
    ILogger<ScheduleSlotApplicationService> logger) : IScheduleSlotApplicationService
{
    public async Task<ScheduleSlotDto> CreateAsync(CreateScheduleSlotDto dto, string keycloakId, CancellationToken ct)
    {
        var validationResult = await createSlotValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var doctor = await doctorRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль врача не найден.");

        if (!doctor.IsActive)
            throw new BusinessRuleException("Нельзя управлять расписанием: профиль врача деактивирован.");

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var hasOverlap = await slotRepository.HasOverlappingSlotAsync(doctor.Id, dto.StartTime, dto.EndTime, null, ct);
            if (hasOverlap)
                throw new BusinessRuleException("Слот пересекается с существующим.");

            var slot = ScheduleSlot.Create(doctor.Id, dto.StartTime, dto.EndTime);
            await slotRepository.AddAsync(slot, ct);

            await unitOfWork.CommitAsync(ct);

            logger.LogInformation(
                "Schedule slot created: {SlotId}, DoctorId={DoctorId}, Start={StartTime:o}, End={EndTime:o}",
                slot.Id, doctor.Id, slot.StartTime, slot.EndTime);

            return MapToDto(slot);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<ScheduleSlotDto> UpdateAsync(Guid id, UpdateScheduleSlotDto dto, string keycloakId, CancellationToken ct)
    {
        var validationResult = await updateSlotValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var doctor = await doctorRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль врача не найден.");

        if (!doctor.IsActive)
            throw new BusinessRuleException("Нельзя управлять расписанием: профиль врача деактивирован.");

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var slot = await slotRepository.GetByIdWithLockAsync(id, ct)
                ?? throw new NotFoundException($"Слот {id} не найден.");

            if (slot.DoctorId != doctor.Id)
                throw new ForbiddenException("Нет доступа к этому слоту.");

            if (slot.Status != SlotStatus.Available)
                throw new BusinessRuleException("Нельзя редактировать слот: он уже забронирован.");

            var hasOverlap = await slotRepository.HasOverlappingSlotAsync(doctor.Id, dto.StartTime, dto.EndTime, slot.Id, ct);
            if (hasOverlap)
                throw new BusinessRuleException("Слот пересекается с существующим расписанием.");

            slot.Update(dto.StartTime, dto.EndTime);
            await slotRepository.UpdateAsync(slot, ct);

            await unitOfWork.CommitAsync(ct);

            logger.LogInformation(
                "Schedule slot updated: {SlotId}, DoctorId={DoctorId}, Start={StartTime:o}, End={EndTime:o}",
                slot.Id, doctor.Id, slot.StartTime, slot.EndTime);

            return MapToDto(slot);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, string keycloakId, CancellationToken ct)
    {
        var doctor = await doctorRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль врача не найден.");

        if (!doctor.IsActive)
            throw new BusinessRuleException("Нельзя управлять расписанием: профиль врача деактивирован.");

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var slot = await slotRepository.GetByIdWithLockAsync(id, ct)
                ?? throw new NotFoundException($"Слот {id} не найден.");

            if (slot.DoctorId != doctor.Id)
                throw new ForbiddenException("Нет доступа к этому слоту.");

            if (slot.Status != SlotStatus.Available)
                throw new BusinessRuleException("Удалить можно только свободный слот.");

            if (await appointmentRepository.ExistsBySlotIdAsync(slot.Id, ct))
                throw new BusinessRuleException("Нельзя удалить слот: к нему привязана запись.");

            await slotRepository.DeleteAsync(slot, ct);

            await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        logger.LogInformation(
            "Schedule slot deleted: {SlotId}, DoctorId={DoctorId}",
            id, doctor.Id);
    }

    public async Task<IReadOnlyList<ScheduleSlotDto>> GetScheduleAsync(string keycloakId, CancellationToken ct)
    {
        var doctor = await doctorRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль врача не найден.");

        if (!doctor.IsActive)
            throw new BusinessRuleException("Нельзя управлять расписанием: профиль врача деактивирован.");

        var slots = await slotRepository.GetByDoctorIdAsync(doctor.Id, ct);
        return slots.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ScheduleSlotDto>> GetAvailableAsync(Guid doctorId, DateTime date, CancellationToken ct)
    {
        var doctor = await doctorRepository.GetByIdAsync(doctorId, ct)
            ?? throw new NotFoundException("Врач не найден.");

        if (!doctor.IsActive)
            throw new NotFoundException("Врач не найден.");

        var slots = await slotRepository.GetAvailableByDoctorIdAsync(doctorId, date, ct);
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
