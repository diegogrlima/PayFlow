namespace PayFlow.Features.Authentication.DTOs
{
    public record LoginRequest(
        string Email,
        string Password);
}
