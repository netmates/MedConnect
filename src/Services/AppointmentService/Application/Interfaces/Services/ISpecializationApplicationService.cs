using AppointmentService.Application.DTOs.Specialization;

namespace AppointmentService.Application.Interfaces.Services;

public interface ISpecializationApplicationService
{
    /// <summary>
    /// Вернуть список всех специализаций
    /// </summary>
    Task<IReadOnlyList<SpecializationDto>> GetAllAsync(CancellationToken ct);
    /// <summary>
    /// Создать новую специализацию
    /// </summary>
    Task<SpecializationDto> CreateAsync(CreateSpecializationDto dto, CancellationToken ct);
    /// <summary>
    /// Обновить специализацию
    /// </summary>
    Task<SpecializationDto> UpdateAsync(Guid id, UpdateSpecializationDto dto, CancellationToken ct);
    /// <summary>
    /// Удалить специализацию
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct);
}
