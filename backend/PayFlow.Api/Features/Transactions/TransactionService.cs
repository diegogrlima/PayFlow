using FluentValidation;
using PayFlow.Features.Transactions.DTOs;
using PayFlow.Domain.Entities;
using PayFlow.Common.Exceptions;
using PayFlow.Infrastructure.Repositories.Interfaces;
using PayFlow.Features.Authentication.Interfaces;

namespace PayFlow.Features.Transactions
{
    public class TransactionService(ITransactionRepository repository,
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateTransactionRequest> transactionValidator)
    {
        public async Task<TransactionResponse> AddTransactionAsync(
            Guid sourceAccountId,
            CreateTransactionRequest request,
            CancellationToken cancellationToken)
        {
            await transactionValidator.ValidateAndThrowAsync(
                request,
                cancellationToken);

            string destinationKey;
            try { destinationKey = TransferKeyNormalizer.Normalize(request.DestinationKeyType, request.DestinationKey); }
            catch (ArgumentException) { throw new ValidationException([new FluentValidation.Results.ValidationFailure(nameof(request.DestinationKey), "Tipo ou formato da chave de destino inv\u00e1lido.")]); }
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var sourceAccount = await accountRepository.GetByIdAndUserIdAsync(
                    sourceAccountId,
                    currentUser.UserId,
                    cancellationToken);

                if (sourceAccount is null)
                {
                    throw new ResourceNotFoundException(
                        $"Conta de origem '{sourceAccountId}' não encontrada.");
                }

                var destinationAccount = await accountRepository.GetByTransferKeyAsync(
                    request.DestinationKeyType, destinationKey,
                    cancellationToken);

                if (destinationAccount is null)
                {
                    throw new ResourceNotFoundException(
                        "Chave de destino nao encontrada.");
                }

                if (sourceAccount.Id == destinationAccount.Id)
                    throw new BusinessRuleException("A conta de destino deve ser diferente da origem.");

                if (sourceAccount.Balance < request.Amount)
                {
                    throw new BusinessRuleException(
                        "Saldo insuficiente para realizar a transferência.");
                }

                sourceAccount.Debit(request.Amount);

                destinationAccount.Deposit(request.Amount);

                var transaction = new Transaction(sourceAccount.Id, destinationAccount.Id, request.Amount);

                await repository.AddAsync(transaction, cancellationToken);

                await unitOfWork.CommitAsync(cancellationToken);

                var response = ToResponse(transaction);

                return response;
            }

            catch
            {
                await unitOfWork.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        public async Task<TransactionResponse> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var transaction = await repository.GetByIdAsync(id, cancellationToken)
                ?? throw new ResourceNotFoundException(
                    $"Transação '{id}' não encontrada.");

            var userId = currentUser.UserId;
            var sourceAccount = await accountRepository.GetByIdAndUserIdAsync(
                transaction.SourceAccountId,
                userId,
                cancellationToken);
            var destinationAccount = await accountRepository.GetByIdAndUserIdAsync(
                transaction.DestinationAccountId,
                userId,
                cancellationToken);

            if (sourceAccount is null && destinationAccount is null)
            {
                throw new ResourceNotFoundException(
                    $"Transação '{id}' não encontrada.");
            }

            return ToResponse(transaction);
        }

        public async Task<IEnumerable<TransactionResponse>> GetAllTransactionsAsync(
            Guid accountId,
            string? type,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            _ = await accountRepository.GetByIdAndUserIdAsync(
                accountId,
                currentUser.UserId,
                cancellationToken)
                ?? throw new ResourceNotFoundException(
                    $"Conta '{accountId}' não encontrada.");

            if (page < 1)
                page = 1;

            if (pageSize < 10 || pageSize > 100)
                pageSize = 10;

            var transactions = await repository.GetHistoryByAccountIdAsync(
                accountId,
                type,
                page,
                pageSize,
                cancellationToken);

            return transactions.Select(ToResponse);
        }

        private static TransactionResponse ToResponse(Transaction transaction) =>
            new(
                transaction.Id,
                transaction.SourceAccountId,
                transaction.DestinationAccountId,
                transaction.Amount,
                transaction.Status.ToString(),
                transaction.CreatedAtUtc);
    }
}
