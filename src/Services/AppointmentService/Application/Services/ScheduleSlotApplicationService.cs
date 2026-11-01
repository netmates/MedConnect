using AppointmentService.Application.DTOs.ScheduleSlot;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Caching;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using FluentValidation;
using MedConnect.Shared.Auth;

namespace AppointmentService.Application.Services;

public sealed class ScheduleSlotApplicationService(
    IAppointmentRepository appointmentRepository,
    IScheduleSlotRepository slotRepository,
    IDoctorRepository doctorRepository,
    IDoctorAvailableSlotCache doctorAvailableSlotCache,
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
            await DropSlotCacheAsync(doctor.Id, slot.StartTime);

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

            var oldStart = slot.StartTime;
            slot.Update(dto.StartTime, dto.EndTime);
            await slotRepository.UpdateAsync(slot, ct);

            await unitOfWork.CommitAsync(ct);
            await DropSlotCacheAsync(doctor.Id, oldStart);
            if (oldStart.Year != slot.StartTime.Year
                || oldStart.Month != slot.StartTime.Month
                || oldStart.Day != slot.StartTime.Day)
            {
                await DropSlotCacheAsync(doctor.Id, slot.StartTime);
            }

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
                throw new BusinessRuleException("Нельзя удалить слот: по нему есть отмененная запись.");

            await slotRepository.DeleteAsync(slot, ct);

            await unitOfWork.CommitAsync(ct);
            await DropSlotCacheAsync(doctor.Id, slot.StartTime);
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

    public async Task<IReadOnlyList<ScheduleSlotDto>> GetScheduleAsync(
        string keycloakId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct)
    {
        var doctor = await doctorRepository.GetByKeycloakIdAsync(keycloakId, ct)
            ?? throw new NotFoundException("Профиль врача не найден.");

        if (!doctor.IsActive)
            throw new BusinessRuleException("Нельзя управлять расписанием: профиль врача деактивирован.");

        var slots = await slotRepository.GetByDoctorIdAsync(doctor.Id, from, to, ct);
        return slots.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ScheduleSlotDto>> GetAvailableAsync(Guid doctorId, DateTime date, CancellationToken ct)
    {
        var doctor = await doctorRepository.GetByIdAsync(doctorId, ct)
            ?? throw new NotFoundException("Врач не найден.");

        if (!doctor.IsActive)
            throw new NotFoundException("Врач не найден.");

        try
        {
            var cached = await doctorAvailableSlotCache.GetAsync(doctorId, date, ct);
            if (cached is not null)
                return WithoutPast(cached);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read doctor slots cache");
        }

        var slots = await slotRepository.GetAvailableByDoctorIdAsync(doctorId, date, ct);
        var result = slots.Select(MapToDto).ToList();

        try
        {
            await doctorAvailableSlotCache.SetAsync(doctorId, date, result, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write doctor slots cache");
        }

        return result;
    }

    private static List<ScheduleSlotDto> WithoutPast(IReadOnlyList<ScheduleSlotDto> slots)
    {
        var now = DateTime.UtcNow;
        return slots.Where(slot => slot.StartTime > now).ToList();
    }

    private async Task DropSlotCacheAsync(Guid doctorId, DateTime slotStart)
    {
        try
        {
            await doctorAvailableSlotCache.RemoveAsync(doctorId, slotStart, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to drop doctor slots cache. DoctorId={DoctorId}, Day={Day:yyyy-MM-dd}",
                doctorId, slotStart);
        }
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
