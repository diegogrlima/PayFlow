namespace PayFlow.Features.Authentication.DTOs
{
    public record LoginResponse(
        string AccessToken,
        string TokenType,
        int ExpiresIn);
}
