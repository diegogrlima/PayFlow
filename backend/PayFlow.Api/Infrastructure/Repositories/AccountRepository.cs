using Microsoft.EntityFrameworkCore;
using PayFlow.Infrastructure.Persistence;
using PayFlow.Domain.Entities;
using PayFlow.Infrastructure.Repositories.Interfaces;

namespace PayFlow.Infrastructure.Repositories
{
    public class AccountRepository(PayFlowDbContext dbContext) : IAccountRepository
    {
        public async Task<IReadOnlyList<Account>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => await dbContext.Accounts.AsNoTracking().Where(a => a.UserId == userId).OrderBy(a => a.CreatedAtUtc).ToListAsync(cancellationToken);

        public Task<Account?> GetByTransferKeyAsync(TransferKeyType type, string key, CancellationToken cancellationToken = default)
            => dbContext.Accounts.SingleOrDefaultAsync(a => a.TransferKeyType == type && a.TransferKey == key, cancellationToken);

        public Task<Account?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return dbContext.Accounts.SingleOrDefaultAsync(
                account => account.Id == id,
                cancellationToken);
        }

        public Task<Account?> GetByIdAndUserIdAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return dbContext.Accounts.SingleOrDefaultAsync(
                account => account.Id == id && account.UserId == userId,
                cancellationToken);
        }

        public async Task AddAsync(
            Account account,
            CancellationToken cancellationToken = default)
        {
            await dbContext.Accounts.AddAsync(account, cancellationToken);
            try { await dbContext.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException exception) when (exception.InnerException is Microsoft.Data.SqlClient.SqlException sql
                && sql.Number is 2601 or 2627 && sql.Message.Contains("IX_TB_Accounts_TransferKey"))
            {
                throw new PayFlow.Common.Exceptions.ConflictException("Chave j\u00e1 cadastrada em outra conta.");
            }
        }

        public async Task UpdateAsync(
            Account account,
            CancellationToken cancellationToken = default)
        {
            try { await dbContext.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException exception) when (exception.InnerException is Microsoft.Data.SqlClient.SqlException sql
                && sql.Number is 2601 or 2627 && sql.Message.Contains("IX_TB_Accounts_TransferKey"))
            {
                throw new PayFlow.Common.Exceptions.ConflictException("Chave j\u00e1 cadastrada em outra conta.");
            }
        }
    }
}
