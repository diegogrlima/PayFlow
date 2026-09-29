using System.IdentityModel.Tokens.Jwt;
using PayFlow.Common.Exceptions;
using PayFlow.Features.Authentication.Interfaces;

namespace PayFlow.Infrastructure.Authentication
{
    public class HttpContextCurrentUser(
        IHttpContextAccessor httpContextAccessor) : ICurrentUser
    {
        public bool IsAuthenticated =>
            httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

        public Guid UserId
        {
            get
            {
                var subject = httpContextAccessor.HttpContext?.User
                    .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                if (!Guid.TryParse(subject, out var userId))
                {
                    throw new InvalidCredentialsException(
                        "Usuário não autenticado.");
                }

                return userId;
            }
        }
    }
}
