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
                    $"UsuÃ¡rio '{userId}' nÃ£o encontrado.");

            var account = new Account(userId, request.HolderName, accountType: request.AccountType);

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

        public async Task<IReadOnlyList<AccountResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var accounts = await repository.GetByUserIdAsync(currentUser.UserId, cancellationToken);
            return accounts.Select(ToResponse).ToList();
        }

        public async Task<AccountResponse> SetTransferKeyAsync(Guid id, SetTransferKeyRequest request, CancellationToken cancellationToken)
        {
            var account = await repository.GetByIdAndUserIdAsync(id, currentUser.UserId, cancellationToken)
                ?? throw new ResourceNotFoundException("Conta n\u00e3o encontrada.");
            try { account.SetTransferKey(request.Type, request.Value); }
            catch (ArgumentException) { throw new ValidationException([new FluentValidation.Results.ValidationFailure(nameof(request.Value), "Tipo ou formato de chave inv\u00e1lido ou incompat\u00edvel com a conta.")]); }
            var existing = await repository.GetByTransferKeyAsync(request.Type, account.TransferKey!, cancellationToken);
            if (existing is not null && existing.Id != id)
                throw new ConflictException("Chave j\u00e1 cadastrada em outra conta.");
            await repository.UpdateAsync(account, cancellationToken);
            return ToResponse(account);
        }

        public async Task RemoveTransferKeyAsync(Guid id, CancellationToken cancellationToken)
        {
            var account = await repository.GetByIdAndUserIdAsync(id, currentUser.UserId, cancellationToken)
                ?? throw new ResourceNotFoundException("Conta n\u00e3o encontrada.");
            account.RemoveTransferKey();
            await repository.UpdateAsync(account, cancellationToken);
        }

        private static AccountResponse ToResponse(Account account) =>
        new(
            account.Id,
            account.UserId,
            account.HolderName,
            account.Balance,
            account.CreatedAtUtc,
            account.AccountType, account.TransferKeyType,
            TransferKeyNormalizer.Mask(account.TransferKeyType, account.TransferKey),
            account.TransferKey is not null);
    }
}
