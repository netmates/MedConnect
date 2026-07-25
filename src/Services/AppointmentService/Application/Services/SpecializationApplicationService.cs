using AppointmentService.Application.DTOs.Admin;
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
    IValidator<UpdateSpecializationDto> updateSpecializationValidator) : ISpecializationApplicationService
{
    private readonly ISpecializationRepository _specializationRepository = specializationRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IValidator<CreateSpecializationDto> _createSpecializationValidator = createSpecializationValidator;
    private readonly IValidator<UpdateSpecializationDto> _updateSpecializationValidator = updateSpecializationValidator;

    public async Task<IReadOnlyList<SpecializationDto>> GetAllAsync(CancellationToken ct)
    {
        var specializations = await _specializationRepository.GetAllAsync(ct);
        return specializations.Select(MapToDto).ToList();
    }

    public async Task<SpecializationDto> CreateAsync(CreateSpecializationDto dto, CancellationToken ct)
    {
        var validationResult = await _createSpecializationValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var specialization = Specialization.Create(name: dto.Name);
            await _specializationRepository.AddAsync(specialization, ct);

            await _unitOfWork.CommitAsync(ct);

            return MapToDto(specialization);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<SpecializationDto> UpdateAsync(Guid id, UpdateSpecializationDto dto, CancellationToken ct)
    {
        var validationResult = await _updateSpecializationValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var specialization = await _specializationRepository.GetByIdAsync(id, ct)
                ?? throw new NotFoundException($"Специализация {id} не найдена.");

            specialization.Update(name: dto.Name);
            await _specializationRepository.UpdateAsync(specialization, ct);

            await _unitOfWork.CommitAsync(ct);

            return MapToDto(specialization);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var specialization = await _specializationRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Специализация {id} не найдена.");

            var hasLinkedDoctors = await _specializationRepository.HasAnyDoctorsAsync(id, ct);
            if (hasLinkedDoctors)
                throw new BusinessRuleException("Нельзя удалить специализацию: к ней привязаны врачи.");

            await _specializationRepository.DeleteAsync(specialization, ct);

            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    private static SpecializationDto MapToDto(Specialization s) => new()
    {
        Id = s.Id,
        Name = s.Name
    };
}
