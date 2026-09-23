namespace PayFlow.DTOs.Transactions
{
    public record TransactionResponse(
        Guid Id,
        Guid SourceAccountId,
        Guid DestinationAccountId,
        decimal Amount,
        string Status,
        DateTime CreatedAtUtc);
}
