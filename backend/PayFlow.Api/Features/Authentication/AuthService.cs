using FluentValidation;
using Microsoft.Extensions.Options;
using PayFlow.Infrastructure.Authentication;
using PayFlow.Features.Authentication.DTOs;
using PayFlow.Common.Exceptions;
using PayFlow.Infrastructure.Repositories.Interfaces;
using PayFlow.Features.Authentication.Interfaces;

namespace PayFlow.Features.Authentication
{
    public class AuthService(
        IUserRepository repository,
        IPasswordHasher passwordHasher,
        IValidator<LoginRequest> validator,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtOptions)
    {
        public async Task<LoginResponse> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            await validator.ValidateAndThrowAsync(
                request,
                cancellationToken);

            var user = await repository.GetByEmailAsync(
                request.Email,
                cancellationToken);

            if (user is null ||
                !passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                throw new InvalidCredentialsException(
                    "E-mail ou senha inválidos.");
            }

            string accessToken = tokenService.GenerateToken(user);

            return new LoginResponse(
                accessToken,
                "Bearer",
                jwtOptions.Value.ExpirationMinutes * 60);
        }
    }
}
