using Microsoft.EntityFrameworkCore;
using PayFlow.Data;
using PayFlow.Entities;
using PayFlow.Repositories.Interfaces;

namespace PayFlow.Repositories
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
    }
}
