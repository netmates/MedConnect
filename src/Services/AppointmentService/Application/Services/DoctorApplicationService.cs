using AppointmentService.Application.DTOs.Doctor;
using AppointmentService.Application.Exceptions;
using AppointmentService.Application.Interfaces;
using AppointmentService.Application.Interfaces.Repositories;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Entities;
using AppointmentService.Infrastructure.Repositories;
using FluentValidation;

namespace AppointmentService.Application.Services;

public class DoctorApplicationService(
    IDoctorRepository doctorRepository,
    ISpecializationRepository specializationRepository,
    IAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateDoctorDto> createDoctorValidator,
    IValidator<UpdateDoctorDto> updateDoctorValidator) : IDoctorApplicationService
{
    private readonly IDoctorRepository _doctorRepository = doctorRepository;
    private readonly ISpecializationRepository _specializationRepository = specializationRepository;
    private readonly IAppointmentRepository _appointmentRepository = appointmentRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IValidator<CreateDoctorDto> _createDoctorValidator = createDoctorValidator;
    private readonly IValidator<UpdateDoctorDto> _updateDoctorValidator = updateDoctorValidator;
    
    public async Task<IReadOnlyList<DoctorDto>> GetAllAsync(Guid? specializationId, CancellationToken ct)
    {
        var doctors = specializationId.HasValue
            ? await _doctorRepository.GetBySpecializationAsync(specializationId.Value, ct)
            : await _doctorRepository.GetActiveAsync(ct);
        return doctors.Select(MapToDto).ToList();
    }
    
    public async Task<DoctorDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var doctor = await _doctorRepository.GetWithSpecializationsAsync(id, ct)
            ?? throw new NotFoundException($"Врач {id} не найден.");
        return MapToDto(doctor);
    }
    
    public async Task<IReadOnlyList<DoctorDto>> GetAllIncludingInactiveAsync(CancellationToken ct)
    {
        var doctors = await _doctorRepository.GetAllIncludingInactiveAsync(ct);
        return doctors.Select(MapToDto).ToList();
    }
    
    public async Task<DoctorDto> CreateAsync(CreateDoctorDto dto, CancellationToken ct)
    {
        var validationResult = await _createDoctorValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        //var keycloakId = await _keycloakAdminService.CreateUserAsync(
        //    email: dto.Email,
        //    temporaryPassword: dto.TemporaryPassword,
        //    role: "doctor",
        //    ct: ct);
        var keycloakId = Guid.NewGuid().ToString();

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var doctor = Doctor.Create(
                keycloakId: keycloakId,
                lastName: dto.LastName,
                firstName: dto.FirstName,
                middleName: dto.MiddleName,
                description: dto.Description,
                experienceYears: dto.ExperienceYears);
            await _doctorRepository.AddAsync(doctor, ct);
            
            foreach (var specializationId in dto.SpecializationIds)
            {
                var specialization = await _specializationRepository.GetByIdAsync(specializationId, ct)
                    ?? throw new NotFoundException($"Специализация {specializationId} не найдена.");

                await _doctorRepository.AddDoctorSpecializationAsync(
                    DoctorSpecialization.Create(doctor.Id, specialization.Id), ct);
            }

            await _unitOfWork.CommitAsync(ct);

            var createdDoctor = await _doctorRepository.GetWithSpecializationsAsync(doctor.Id, ct);
            return MapToDto(createdDoctor!);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            //try { await _keycloakAdminService.DeleteUserAsync(keycloakId, ct); }
            //catch (Exception cleanupEx)
            //{   
            //}
            throw;
        }
    }
    
    public async Task<DoctorDto> UpdateAsync(Guid id, UpdateDoctorDto dto, CancellationToken ct)
    {
        var validationResult = await _updateDoctorValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var doctor = await _doctorRepository.GetWithSpecializationsAsync(id, ct)
            ?? throw new NotFoundException($"Врач {id} не найден.");

            doctor.Update(
                lastName: dto.LastName,
                firstName: dto.FirstName,
                middleName: dto.MiddleName,
                description: dto.Description,
                experienceYears: dto.ExperienceYears);
            await _doctorRepository.UpdateAsync(doctor, ct);

            foreach (var specializationId in dto.SpecializationIds)
            {
                _ = await _specializationRepository.GetByIdAsync(specializationId, ct)
                    ?? throw new NotFoundException($"Специализация {specializationId} не найдена.");
            }

            var currentIds = doctor.DoctorSpecializations
                                .Select(ds => ds.SpecializationId)
                                .ToHashSet();
            var desiredIds = dto.SpecializationIds.ToHashSet();

            if (desiredIds.Count == 0)
                throw new BusinessRuleException("Врач должен иметь хотя бы одну специализацию.");

            var toAdd = desiredIds.Except(currentIds);
            var toRemove = currentIds.Except(desiredIds);

            foreach (var specializationId in toAdd)
            {
                await _doctorRepository.AddDoctorSpecializationAsync(
                    DoctorSpecialization.Create(doctor.Id, specializationId), ct);
            }

            foreach (var specializationId in toRemove)
            {
                await _doctorRepository.RemoveDoctorSpecializationAsync(doctor.Id, specializationId, ct);
            }

            await _unitOfWork.CommitAsync(ct);

            var updateDoctor = await _doctorRepository.GetWithSpecializationsAsync(doctor.Id, ct);
            return MapToDto(updateDoctor!);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
    
    public async Task DeactivateAsync(Guid id, CancellationToken ct)
    {
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var doctor = await _doctorRepository.GetByIdAsync(id, ct)
                ?? throw new NotFoundException($"Врач {id} не найден.");

            var hasFuture = await _appointmentRepository
                .HasActiveFutureAppointmentsByDoctorAsync(doctor.Id, DateTime.UtcNow, ct);
            if (hasFuture)
                throw new BusinessRuleException(
                    "Невозможно деактивировать врача: есть активные будущие записи. " +
                    "Сначала отмените все запланированные приёмы.");

            doctor.Deactivate();
            await _doctorRepository.UpdateAsync(doctor, ct);

            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        //await _keycloakAdminService.DisableUserAsync(doctor.KeycloakId, ct);
    }
    
    public async Task ActivateAsync(Guid id, CancellationToken ct)
    {
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var doctor = await _doctorRepository.GetByIdAsync(id, ct)
                ?? throw new NotFoundException($"Врач {id} не найден.");

            doctor.Activate();
            await _doctorRepository.UpdateAsync(doctor, ct);

            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        //await _keycloakAdminService.EnableUserAsync(doctor.KeycloakId, ct);
    }

    // Реализовать public async Task ResetPasswordAsync

    private static DoctorDto MapToDto(Doctor d) => new()
    {
        Id = d.Id,
        LastName = d.LastName,
        FirstName = d.FirstName,
        MiddleName = d.MiddleName,
        Description = d.Description,
        ExperienceYears = d.ExperienceYears,
        IsActive = d.IsActive,
        Specializations = d.DoctorSpecializations
            .Select(ds => ds.Specialization.Name)
            .ToList()
    };
}
