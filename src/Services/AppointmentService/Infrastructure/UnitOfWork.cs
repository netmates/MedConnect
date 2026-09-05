using AppointmentService.Application.Interfaces;
using AppointmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace AppointmentService.Infrastructure;

public class UnitOfWork(AppointmentDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    // SaveChanges только из CommitAsync — чтобы не сохранять вне транзакции
    private async Task<int> SaveChangesInternalAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException(
                "Транзакция уже активна. Вызовите CommitAsync или RollbackAsync перед началом новой транзакции.");
        _transaction = await context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("Транзакция не была начата.");

        await SaveChangesInternalAsync(ct);
        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;

        try
        {
            await _transaction.RollbackAsync(ct);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
