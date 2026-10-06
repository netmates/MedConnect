namespace AppointmentService.Application.Interfaces;

public interface IUnitOfWork
{
    public Task BeginTransactionAsync(CancellationToken ct = default);
    public Task CommitAsync(CancellationToken ct = default);
    public Task RollbackAsync(CancellationToken ct = default);
}
