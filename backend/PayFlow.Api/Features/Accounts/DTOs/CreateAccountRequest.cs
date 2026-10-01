namespace PayFlow.Features.Accounts.DTOs
{
    public record CreateAccountRequest(string HolderName, PayFlow.Domain.Entities.AccountType AccountType);

}
