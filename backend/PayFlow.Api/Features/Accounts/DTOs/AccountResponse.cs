namespace PayFlow.Features.Accounts.DTOs
{
    public record AccountResponse(
        Guid id,
        string holderName,
        decimal balance,
        DateTime createdAtUtc);


}
