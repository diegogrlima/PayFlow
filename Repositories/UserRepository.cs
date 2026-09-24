using Microsoft.EntityFrameworkCore;
using PayFlow.Data;
using PayFlow.Entities;
using PayFlow.Repositories.Interfaces;

namespace PayFlow.Repositories
{
    public class UserRepository(PayFlowDbContext dbContext) : IUserRepository
    {
        public async Task AddAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            await dbContext.Users.AddAsync(user, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public Task<bool> ExistsByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            var normalizedEmail = email.Trim();

            return dbContext.Users.AnyAsync(
                user => user.Email == normalizedEmail,
                cancellationToken);
        }

        public Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            var normalizedEmail = email.Trim();

            return dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    user => user.Email == normalizedEmail,
                    cancellationToken);
        }

        public Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    user => user.Id == id,
                    cancellationToken);
        }
    }
}
