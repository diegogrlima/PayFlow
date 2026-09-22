using PayFlow.DTOs.Account;
using PayFlow.Entities;
using PayFlow.Repositories.Interfaces;

namespace PayFlow.Services
{
    public class AccountService(IAccountRepository repository)
    {

        public async Task<Account> CreateAsync(
            CreateAccountRequest request,
            CancellationToken cancellationToken = default)
        {
            Account account = new(request.HolderName);

            await repository.AddAsync(account, cancellationToken);

            return account;

        }

        public Task<Account?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return repository.GetByIdAsync(id, cancellationToken);
        }
    }
}
