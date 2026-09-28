using PayFlow.Entities;

namespace PayFlow.Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}
