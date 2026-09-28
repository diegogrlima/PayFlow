namespace PayFlow.DTOs.Users
{
    public record LoginResponse(
        string AccessToken,
        string TokenType,
        int ExpiresIn);
}
