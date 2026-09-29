namespace PayFlow.Features.Authentication.DTOs
{
    public record LoginResponse(
        string AccessToken,
        string RefreshToken,
        string TokenType,
        int ExpiresIn);
}
