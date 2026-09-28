using PayFlow.Infrastructure.Persistence;
using PayFlow.Infrastructure.Repositories.Interfaces;

namespace PayFlow.Infrastructure.Repositories
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
