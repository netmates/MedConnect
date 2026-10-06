namespace AppointmentService.Application.Interfaces.Repositories;

public interface IRepository<T> where T : class
{
    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    public Task AddAsync(T entity, CancellationToken ct = default);
    public Task UpdateAsync(T entity, CancellationToken ct = default);
    public Task DeleteAsync(T entity, CancellationToken ct = default);
}
