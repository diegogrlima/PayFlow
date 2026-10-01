namespace PayFlow.Features.Accounts.DTOs
{
    public record AccountResponse(
        Guid id,
        Guid userId,
        string holderName,
        decimal balance,
        DateTime createdAtUtc,
        PayFlow.Domain.Entities.AccountType accountType,
        PayFlow.Domain.Entities.TransferKeyType? transferKeyType,
        string? transferKey,
        bool hasTransferKey);


}
