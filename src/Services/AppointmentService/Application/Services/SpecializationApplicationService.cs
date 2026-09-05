using AppointmentService.Application.DTOs.Specialization;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Entities;
using FluentValidation;

namespace AppointmentService.Application.Services;

public class SpecializationApplicationService(
    ISpecializationRepository specializationRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateSpecializationDto> createSpecializationValidator,
    IValidator<UpdateSpecializationDto> updateSpecializationValidator,
    ILogger<SpecializationApplicationService> logger) : ISpecializationApplicationService
{
    public async Task<IReadOnlyList<SpecializationDto>> GetAllAsync(CancellationToken ct)
    {
        var specializations = await specializationRepository.GetAllAsync(ct);
        return specializations.Select(MapToDto).ToList();
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

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var specialization = await specializationRepository.GetByIdAsync(id, ct)
                ?? throw new NotFoundException($"Специализация {id} не найдена.");

            specialization.Update(name: dto.Name);
            await specializationRepository.UpdateAsync(specialization, ct);

            await unitOfWork.CommitAsync(ct);

            logger.LogInformation(
                "Specialization updated: {SpecializationId}, Name={Name}",
                specialization.Id, specialization.Name);

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
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        logger.LogInformation("Specialization deleted: {SpecializationId}", id);
    }

    private static SpecializationDto MapToDto(Specialization s) => new()
    {
        Id = s.Id,
        Name = s.Name
    };
}
