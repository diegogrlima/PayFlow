namespace PayFlow.Features.Transactions.DTOs
{
    public record CreateTransactionRequest(
        Guid SourceAccountId,
        Guid DestinationAccountId,
        decimal Amount);
}
