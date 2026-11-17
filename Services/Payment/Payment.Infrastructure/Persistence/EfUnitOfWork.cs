using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Payment.Application.Interfaces;

namespace Payment.Infrastructure.Persistence
{
    public sealed class EfUnitOfWork : IUnitOfWork, IAsyncDisposable
    {
        private readonly PaymentDbContext _db;
        private IDbContextTransaction? _transaction;

        public EfUnitOfWork(PaymentDbContext db) => _db = db;

        public async Task BeginTransactionAsync(CancellationToken ct = default)
        {
            if (_transaction is not null)
                throw new InvalidOperationException("A transaction is already active.");

            _transaction = await _db.Database.BeginTransactionAsync(ct);
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => _db.SaveChangesAsync(ct);

        public async Task CommitTransactionAsync(CancellationToken ct = default)
        {
            if (_transaction is null)
                throw new InvalidOperationException("No active transaction.");

            await _db.SaveChangesAsync(ct);
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        public async Task RollbackTransactionAsync(CancellationToken ct = default)
        {
            if (_transaction is null) return;

            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction is not null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }
}

