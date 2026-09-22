namespace PayFlow.DTOs.Account
{
    public record AccountResponse(
        Guid id,
        string holderName,
        decimal balance,
        DateTime createdAtUtc);


}
