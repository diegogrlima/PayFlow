namespace PayFlow.DTOs.Transactions
{
    public record CreateTransactionRequest(
        Guid SourceAccountId,
        Guid DestinationAccountId,
        decimal Amount);
}
