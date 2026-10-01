using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PayFlow.Domain.Entities;
using PayFlow.Features.Authentication.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PayFlow.Infrastructure.Authentication
{
    public class JwtTokenService(
        IOptions<JwtSettings> options,
        TimeProvider? timeProvider = null) : ITokenService
    {
        private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
        public string GenerateToken(User user)
        {
            var keyBytes = Convert.FromBase64String(
                options.Value.Key);

            var claims = new[]
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    user.Id.ToString()),
                new Claim(
                    JwtRegisteredClaimNames.Email,
                    user.Email)
            };

            var securityKey = new SymmetricSecurityKey(keyBytes);

            var credentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

            var expires = clock.GetUtcNow().UtcDateTime.AddMinutes(
                options.Value.ExpirationMinutes);

            var token = new JwtSecurityToken(
                issuer: options.Value.Issuer,
                audience: options.Value.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler()
                .WriteToken(token);

            return tokenString;
        }

        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        public string HashRefreshToken(string token)
        {
            return Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }
    }
}
