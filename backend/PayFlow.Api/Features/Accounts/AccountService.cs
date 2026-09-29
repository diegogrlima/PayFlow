using FluentValidation;
using PayFlow.Features.Accounts.DTOs;
using PayFlow.Domain.Entities;
using PayFlow.Common.Exceptions;
using PayFlow.Infrastructure.Repositories.Interfaces;
using PayFlow.Features.Authentication.Interfaces;

namespace PayFlow.Features.Accounts
{
    public class AccountService(IAccountRepository repository,
        IUserRepository userRepository,
        ICurrentUser currentUser,
        IValidator<CreateAccountRequest> accountValidator,
        IValidator<CreateDepositRequest> depositValidator)
    {

        public async Task<AccountResponse> CreateAsync(
            CreateAccountRequest request,
            CancellationToken cancellationToken = default)
        {
            await accountValidator.ValidateAndThrowAsync(request, cancellationToken);

            var userId = currentUser.UserId;

            _ = await userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new ResourceNotFoundException(
                    $"Usuário '{userId}' não encontrado.");

            var account = new Account(userId, request.HolderName);

            await repository.AddAsync(account, cancellationToken);

            return ToResponse(account);

        }

        public async Task<AccountResponse?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {

            var account = await repository.GetByIdAndUserIdAsync(
                id,
                currentUser.UserId,
                cancellationToken)
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

            var account = await repository.GetByIdAndUserIdAsync(
                id,
                currentUser.UserId,
                cancellationToken)
                ?? throw new ResourceNotFoundException(nameof(id));

            account.Deposit(request.Amount);

            await repository.UpdateAsync(account, cancellationToken);

            return account.Balance;
        }

        private static AccountResponse ToResponse(Account account) =>
        new(
            account.Id,
            account.UserId,
            account.HolderName,
            account.Balance,
            account.CreatedAtUtc);
    }
}
