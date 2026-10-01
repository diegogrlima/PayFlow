using PayFlow.Domain.Entities;
namespace PayFlow.Features.Transactions.DTOs;
public record CreateTransactionRequest(TransferKeyType DestinationKeyType, string DestinationKey, decimal Amount);
