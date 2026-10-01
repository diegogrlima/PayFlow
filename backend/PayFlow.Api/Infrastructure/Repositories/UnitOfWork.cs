using Microsoft.EntityFrameworkCore;
using PayFlow.Infrastructure.Persistence;
using PayFlow.Infrastructure.Repositories.Interfaces;

namespace PayFlow.Infrastructure.Repositories
{
    public class UnitOfWork(PayFlowDbContext dbContext) : IUnitOfWork
    {
        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            var transaction = dbContext.Database.CurrentTransaction
                ?? throw new InvalidOperationException("No active transaction.");
            await transaction.CommitAsync(cancellationToken);
            await transaction.DisposeAsync();
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            var transaction = dbContext.Database.CurrentTransaction;
            if (transaction is null) return;
            try { await transaction.RollbackAsync(cancellationToken); }
            finally { await transaction.DisposeAsync(); }
        }
    }
}
