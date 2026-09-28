using FluentValidation;
using Microsoft.Extensions.Options;
using PayFlow.Configurations;
using PayFlow.DTOs.Users;
using PayFlow.Exceptions;
using PayFlow.Repositories.Interfaces;
using PayFlow.Services.Interfaces;

namespace PayFlow.Services
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
