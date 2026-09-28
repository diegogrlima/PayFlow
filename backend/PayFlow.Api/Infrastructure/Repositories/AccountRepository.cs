using Microsoft.EntityFrameworkCore;
using PayFlow.Infrastructure.Persistence;
using PayFlow.Domain.Entities;
using PayFlow.Infrastructure.Repositories.Interfaces;

namespace PayFlow.Infrastructure.Repositories
{
    public class AccountRepository(PayFlowDbContext dbContext) : IAccountRepository
    {
        public Task<Account?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return dbContext.Accounts.SingleOrDefaultAsync(
                account => account.Id == id,
                cancellationToken);
        }

        public async Task AddAsync(
            Account account,
            CancellationToken cancellationToken = default)
        {
            await dbContext.Accounts.AddAsync(account, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(
            Account account,
            CancellationToken cancellationToken = default)
        {
            dbContext.Accounts.Update(account);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
