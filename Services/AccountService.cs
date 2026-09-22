using FluentValidation;
using PayFlow.DTOs.Account;
using PayFlow.Entities;
using PayFlow.Exceptions;
using PayFlow.Repositories.Interfaces;

namespace PayFlow.Services
{
    public class AccountService(IAccountRepository repository,
        IValidator<CreateAccountRequest> accountValidator,
        IValidator<CreateDepositRequest> depositValidator)
    {

        public async Task<AccountResponse> CreateAsync(
            CreateAccountRequest request,
            CancellationToken cancellationToken = default)
        {
            await accountValidator.ValidateAndThrowAsync(request, cancellationToken);

            var account = new Account(request.HolderName);

            await repository.AddAsync(account, cancellationToken);

            return ToResponse(account);

        }

        public async Task<AccountResponse?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {

            var account = await repository.GetByIdAsync(id, cancellationToken)
                ?? throw new ResourceNotFoundException(nameof(id));

            return ToResponse(account);
        }

        public async Task<decimal> AddDepositAsync(
            Guid id,
            CreateDepositRequest request,
            CancellationToken cancellationToken = default)
        {
            await depositValidator.ValidateAndThrowAsync(
                request,
                cancellationToken: cancellationToken);

            var account = await repository.GetByIdAsync(id, cancellationToken)
                ?? throw new ResourceNotFoundException(nameof(id));

            account.Deposit(request.Amount);

            await repository.UpdateAsync(account, cancellationToken);

            return account.Balance;
        }

        private static AccountResponse ToResponse(Account account) =>
        new(
            account.Id,
            account.HolderName,
            account.Balance,
            account.CreatedAtUtc);
    }
}
