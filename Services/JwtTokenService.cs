using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PayFlow.Configurations;
using PayFlow.Entities;
using PayFlow.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PayFlow.Services
{
    public class JwtTokenService(
        IOptions<JwtSettings> options) : ITokenService
    {
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

            var expires = DateTime.UtcNow.AddMinutes(
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
    }
}
