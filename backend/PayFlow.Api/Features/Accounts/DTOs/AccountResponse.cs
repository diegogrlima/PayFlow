namespace PayFlow.Features.Accounts.DTOs
{
    public record AccountResponse(
        Guid id,
        Guid userId,
        string holderName,
        decimal balance,
        DateTime createdAtUtc);


}
