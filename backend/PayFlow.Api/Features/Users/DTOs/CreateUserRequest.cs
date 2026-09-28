namespace PayFlow.Features.Users.DTOs
{
    public record CreateUserRequest(
        string Email,
        string Password);
}
