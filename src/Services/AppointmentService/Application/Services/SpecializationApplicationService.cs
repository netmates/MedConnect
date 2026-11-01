using AppointmentService.Application.DTOs.Specialization;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Caching;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Entities;
using FluentValidation;

namespace AppointmentService.Application.Services;

public sealed class SpecializationApplicationService(
    ISpecializationRepository specializationRepository,
    ISpecializationListCache specializationListCache,
    IDoctorListCache doctorListCache,
    IUnitOfWork unitOfWork,
    IValidator<CreateSpecializationDto> createSpecializationValidator,
    IValidator<UpdateSpecializationDto> updateSpecializationValidator,
    ILogger<SpecializationApplicationService> logger) : ISpecializationApplicationService
{
    public async Task<IReadOnlyList<SpecializationDto>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            var cached = await specializationListCache.GetAsync(ct);
            if (cached is not null)
                return cached;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read specializations cache");
        }

        var specializations = await specializationRepository.GetAllAsync(ct);
        var result = specializations.Select(MapToDto).ToList();

        try
        {
            await specializationListCache.SetAsync(result, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write specializations cache");
        }

        return result;
    }

    public async Task<SpecializationDto> CreateAsync(CreateSpecializationDto dto, CancellationToken ct)
    {
        var validationResult = await createSpecializationValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var specialization = Specialization.Create(name: dto.Name);
            await specializationRepository.AddAsync(specialization, ct);

            await unitOfWork.CommitAsync(ct);
            await DropCacheAsync();

            logger.LogInformation(
                "Specialization created: {SpecializationId}, Name={Name}",
                specialization.Id, specialization.Name);

            return MapToDto(specialization);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<SpecializationDto> UpdateAsync(Guid id, UpdateSpecializationDto dto, CancellationToken ct)
    {
        var validationResult = await updateSpecializationValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var specialization = await specializationRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Специализация {id} не найдена.");

        var newName = dto.Name.Trim();
        if (string.Equals(specialization.Name, newName, StringComparison.Ordinal))
            return MapToDto(specialization);

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var oldName = specialization.Name;
            specialization.Update(name: newName);
            await specializationRepository.UpdateAsync(specialization, ct);

            await unitOfWork.CommitAsync(ct);
            await DropCacheAsync();
            await DropDoctorListCacheAsync();

            logger.LogInformation(
                "Specialization updated: {SpecializationId}, OldName={OldName}, NewName={NewName}",
                specialization.Id, oldName, newName);

            return MapToDto(specialization);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var specialization = await specializationRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Специализация {id} не найдена.");

            var hasLinkedDoctors = await specializationRepository.HasAnyDoctorsAsync(id, ct);
            if (hasLinkedDoctors)
                throw new BusinessRuleException("Нельзя удалить специализацию: к ней привязаны врачи.");

            await specializationRepository.DeleteAsync(specialization, ct);

            await unitOfWork.CommitAsync(ct);
            await DropCacheAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        logger.LogInformation("Specialization deleted: {SpecializationId}", id);
    }

    private async Task DropCacheAsync()
    {
        try
        {
            await specializationListCache.RemoveAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to drop specializations cache");
        }
    }

    private async Task DropDoctorListCacheAsync()
    {
        try
        {
            await doctorListCache.RemoveAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to drop doctors cache");
        }
    }

    private static SpecializationDto MapToDto(Specialization s) => new()
    {
        Id = s.Id,
        Name = s.Name
    };
}
