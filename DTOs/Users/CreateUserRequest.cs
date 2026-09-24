namespace PayFlow.DTOs.Users
{
    public record CreateUserRequest(
        string Email,
        string Password);
}
