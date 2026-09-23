using PayFlow.Data;
using PayFlow.Repositories.Interfaces;

namespace PayFlow.Repositories
{
    public class UnitOfWork(PayFlowDbContext dbContext) : IUnitOfWork
    {
        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await dbContext.Database.CommitTransactionAsync(cancellationToken);
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await dbContext.Database.RollbackTransactionAsync(cancellationToken);
        }
    }
}
