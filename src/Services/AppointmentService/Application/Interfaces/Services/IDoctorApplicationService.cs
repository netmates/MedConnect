using AppointmentService.Application.DTOs.Doctor;

namespace AppointmentService.Application.Interfaces.Services;

public interface IDoctorApplicationService
{
    /// <summary>
    /// Возвращает список активных врачей.
    /// Если передан specializationId — фильтрует врачей по специализации
    /// </summary>
    Task<IReadOnlyList<DoctorDto>> GetAllAsync(Guid? specializationId, CancellationToken ct);
    /// <summary>
    /// Возвращает врача вместе с его специализациями
    /// </summary>
    Task<DoctorDto> GetByIdAsync(Guid id, CancellationToken ct);
    /// <summary>
    /// Возвращает всех врачей (включая деактивированных) с загруженными специализациями
    /// </summary>
    Task<IReadOnlyList<DoctorDto>> GetAllIncludingInactiveAsync(CancellationToken ct);
    /// <summary>
    /// Создаем доктора со специализациями
    /// </summary>
    Task<DoctorDto> CreateAsync(CreateDoctorDto dto, CancellationToken ct);    
    /// <summary>
    /// Обновляем данные по врачу
    /// </summary>
    Task<DoctorDto> UpdateAsync(Guid id, UpdateDoctorDto dto, CancellationToken ct);
    /// <summary>
    /// Деактивация врача
    /// </summary>
    Task DeactivateAsync(Guid id, CancellationToken ct);
    /// <summary>
    /// Активация врача
    /// </summary>
    Task ActivateAsync(Guid id, CancellationToken ct);
    // Task ResetPasswordAsync(Guid id, ResetPasswordDto dto, CancellationToken ct);
}
