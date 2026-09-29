using FluentValidation;
using Microsoft.Extensions.Options;
using PayFlow.Infrastructure.Authentication;
using PayFlow.Features.Authentication.DTOs;
using PayFlow.Common.Exceptions;
using PayFlow.Infrastructure.Repositories.Interfaces;
using PayFlow.Features.Authentication.Interfaces;
using PayFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace PayFlow.Features.Authentication
{
    public class AuthService(
        IUserRepository repository,
        IPasswordHasher passwordHasher,
        IValidator<LoginRequest> validator,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
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

            return await CreateTokenPairAsync(user, cancellationToken: cancellationToken);
        }

        public async Task<LoginResponse> RefreshAsync(
            RefreshTokenRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                throw InvalidRefreshToken();

            var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
            var currentToken = await refreshTokenRepository.GetByHashAsync(
                tokenHash,
                cancellationToken);

            if (currentToken is null)
                throw InvalidRefreshToken();

            if (currentToken.RevokedAtUtc is not null)
            {
                await refreshTokenRepository.RevokeFamilyAsync(
                    currentToken.FamilyId,
                    "Reutilização de token detectada",
                    cancellationToken);
                throw InvalidRefreshToken();
            }

            if (currentToken.ExpiresAtUtc <= DateTime.UtcNow)
            {
                currentToken.Revoke("Token expirado");
                await refreshTokenRepository.SaveChangesAsync(cancellationToken);
                throw InvalidRefreshToken();
            }

            var user = await repository.GetByIdAsync(
                currentToken.UserId,
                cancellationToken);

            if (user is null)
                throw InvalidRefreshToken();

            try
            {
                return await CreateTokenPairAsync(
                    user,
                    currentToken,
                    cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw InvalidRefreshToken();
            }
        }

        public async Task RevokeAsync(
            RevokeTokenRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return;

            var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
            var refreshToken = await refreshTokenRepository.GetByHashAsync(
                tokenHash,
                cancellationToken);

            if (refreshToken is null || refreshToken.RevokedAtUtc is not null)
                return;

            refreshToken.Revoke("Revogado pelo cliente");

            try
            {
                await refreshTokenRepository.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // A revogação é idempotente; outro pedido já alterou o token.
            }
        }

        private async Task<LoginResponse> CreateTokenPairAsync(
            User user,
            RefreshToken? currentToken = null,
            CancellationToken cancellationToken = default)
        {
            var plainTextRefreshToken = tokenService.GenerateRefreshToken();
            var refreshToken = new RefreshToken(
                user.Id,
                tokenService.HashRefreshToken(plainTextRefreshToken),
                DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationDays),
                currentToken?.FamilyId);

            if (currentToken is not null)
                currentToken.Revoke("Token rotacionado", refreshToken.Id);

            await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
            await refreshTokenRepository.SaveChangesAsync(cancellationToken);

            string accessToken = tokenService.GenerateToken(user);

            return new LoginResponse(
                accessToken,
                plainTextRefreshToken,
                "Bearer",
                jwtOptions.Value.ExpirationMinutes * 60);
        }

        private static InvalidCredentialsException InvalidRefreshToken()
        {
            return new InvalidCredentialsException(
                "Refresh token inválido, expirado ou revogado.");
        }
    }
}
