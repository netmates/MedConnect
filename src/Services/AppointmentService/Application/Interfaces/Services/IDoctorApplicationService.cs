using AppointmentService.Application.DTOs.Doctor;

namespace AppointmentService.Application.Interfaces.Services;

public interface IDoctorApplicationService
{
    /// <summary>
    /// Получить список активных врачей.
    /// Если передан specializationId — фильтрует врачей по специализации.
    /// </summary>
    Task<IReadOnlyList<DoctorDto>> GetAllAsync(Guid? specializationId, CancellationToken ct);
    /// <summary>
    /// Получить врача вместе с его специализациями.
    /// </summary>
    Task<DoctorDto> GetByIdAsync(Guid id, CancellationToken ct);
    /// <summary>
    /// Получить всех врачей (включая деактивированных) с загруженными специализациями.
    /// </summary>
    Task<IReadOnlyList<DoctorDto>> GetAllIncludingInactiveAsync(CancellationToken ct);
    /// <summary>
    /// Создать врача со специализациями.
    /// </summary>
    Task<DoctorDto> CreateAsync(CreateDoctorDto dto, CancellationToken ct);
    /// <summary>
    /// Обновить данные врача.
    /// </summary>
    Task<DoctorDto> UpdateAsync(Guid id, UpdateDoctorDto dto, CancellationToken ct);
    /// <summary>
    /// Деактивировать врача.
    /// </summary>
    Task DeactivateAsync(Guid id, CancellationToken ct);
    /// <summary>
    /// Активировать врача.
    /// </summary>
    Task ActivateAsync(Guid id, CancellationToken ct);
    /// <summary>
    /// Сбросить пароль врача.
    /// </summary>
    Task ResetPasswordAsync(Guid id, ResetPasswordDto dto, CancellationToken ct);
}
