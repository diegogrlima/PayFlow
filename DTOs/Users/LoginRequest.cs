namespace PayFlow.DTOs.Users
{
    public record LoginRequest(
        string Email,
        string Password);
}
