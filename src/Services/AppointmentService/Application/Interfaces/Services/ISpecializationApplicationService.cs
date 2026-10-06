using AppointmentService.Application.DTOs.Specialization;

namespace AppointmentService.Application.Interfaces.Services;

public interface ISpecializationApplicationService
{
    /// <summary>
    /// Получить список всех специализаций.
    /// </summary>
    public Task<IReadOnlyList<SpecializationDto>> GetAllAsync(CancellationToken ct);
    /// <summary>
    /// Создать специализацию.
    /// </summary>
    public Task<SpecializationDto> CreateAsync(CreateSpecializationDto dto, CancellationToken ct);
    /// <summary>
    /// Обновить специализацию.
    /// </summary>
    public Task<SpecializationDto> UpdateAsync(Guid id, UpdateSpecializationDto dto, CancellationToken ct);
    /// <summary>
    /// Удалить специализацию.
    /// </summary>
    public Task DeleteAsync(Guid id, CancellationToken ct);
}
