using Microsoft.EntityFrameworkCore;
using PayFlow.Data;
using PayFlow.Entities;
using PayFlow.Repositories.Interfaces;

namespace PayFlow.Repositories
{
    public class TransactionRepository(PayFlowDbContext dbContext) : ITransactionRepository
    {
        public Task<Transaction?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return dbContext.Transactions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    transaction => transaction.Id == id,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<Transaction>> GetHistoryByAccountIdAsync(
            Guid accountId,
            string? type,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var query = dbContext.Transactions
                .AsNoTracking()
                .AsQueryable();

            query = type switch
            {
                "sent" => query.Where(transaction =>
                     transaction.SourceAccountId == accountId),

                "received" => query.Where(transaction =>
                    transaction.DestinationAccountId == accountId),

                _ => query.Where(transaction =>
                    transaction.SourceAccountId == accountId ||
                    transaction.DestinationAccountId == accountId)
            };

            return await query
                .OrderByDescending(transaction => transaction.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            Transaction transaction,
            CancellationToken cancellationToken = default)
        {
            await dbContext.Transactions.AddAsync(transaction, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
