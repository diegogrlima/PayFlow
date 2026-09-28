namespace PayFlow.Features.Transactions.DTOs
{
    public record TransactionResponse(
        Guid Id,
        Guid SourceAccountId,
        Guid DestinationAccountId,
        decimal Amount,
        string Status,
        DateTime CreatedAtUtc);
}
