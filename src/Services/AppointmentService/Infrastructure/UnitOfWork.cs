using AppointmentService.Application.Interfaces;
using AppointmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace AppointmentService.Infrastructure;

public class UnitOfWork(AppointmentDbContext context) : IUnitOfWork
{
    private readonly AppointmentDbContext _context = context;
    private IDbContextTransaction? _transaction;

    /// <summary>
    /// SaveChangesAsync приватный — вызывается только из CommitAsync.
    /// Это исключает двойное сохранение и утечку изменений в обход транзакции 
    /// </summary>
    private async Task<int> SaveChangesInternalAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException(
                "Транзакция уже активна. Вызовите CommitAsync или RollbackAsync перед началом новой транзакции.");
        _transaction = await _context.Database.BeginTransactionAsync(ct);
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
