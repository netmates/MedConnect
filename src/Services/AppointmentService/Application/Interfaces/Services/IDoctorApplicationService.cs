using AppointmentService.Application.DTOs.Doctor;

namespace AppointmentService.Application.Interfaces.Services;

public interface IDoctorApplicationService
{
    /// <summary>
    /// Получить список активных врачей.
    /// Если передан specializationId — фильтрует врачей по специализации.
    /// </summary>
    public Task<IReadOnlyList<DoctorDto>> GetAllAsync(Guid? specializationId, CancellationToken ct);
    /// <summary>
    /// Получить врача вместе с его специализациями.
    /// </summary>
    public Task<DoctorDto> GetByIdAsync(Guid id, CancellationToken ct);
    /// <summary>
    /// Получить всех врачей (включая деактивированных) с загруженными специализациями.
    /// </summary>
    public Task<IReadOnlyList<DoctorDto>> GetAllIncludingInactiveAsync(CancellationToken ct);
    /// <summary>
    /// Создать врача со специализациями.
    /// </summary>
    public Task<DoctorDto> CreateAsync(CreateDoctorDto dto, CancellationToken ct);
    /// <summary>
    /// Обновить данные врача.
    /// </summary>
    public Task<DoctorDto> UpdateAsync(Guid id, UpdateDoctorDto dto, CancellationToken ct);
    /// <summary>
    /// Деактивировать врача.
    /// </summary>
    public Task DeactivateAsync(Guid id, CancellationToken ct);
    /// <summary>
    /// Активировать врача.
    /// </summary>
    public Task ActivateAsync(Guid id, CancellationToken ct);
    /// <summary>
    /// Сбросить пароль врача.
    /// </summary>
    public Task ResetPasswordAsync(Guid id, ResetPasswordDto dto, CancellationToken ct);
}
