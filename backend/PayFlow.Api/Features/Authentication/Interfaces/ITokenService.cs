using PayFlow.Domain.Entities;

namespace PayFlow.Features.Authentication.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}
