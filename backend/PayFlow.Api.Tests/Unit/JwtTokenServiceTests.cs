using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PayFlow.Domain.Entities;
using PayFlow.Infrastructure.Authentication;

namespace PayFlow.Api.Tests.Unit;

public class JwtTokenServiceTests
{
    [Fact]
    public void GenerateToken_ShouldAuthenticateUserWithConfiguredIssuerAudienceAndLifetime_WhenSettingsAreValid()
    {
        var clock = new ManualTimeProvider();
        var user = new User("user@example.com", "hash");
        var settings = Settings();
        var service = new JwtTokenService(Options.Create(settings), clock);

        var encoded = service.GenerateToken(user);

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = settings.Issuer,
            ValidateAudience = true, ValidAudience = settings.Audience,
            ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(settings.Key)),
            ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
            LifetimeValidator = (_, expires, _, _) => expires == clock.GetUtcNow().UtcDateTime.AddMinutes(15)
        };
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(encoded, parameters, out _);
        principal.FindFirst("sub")!.Value.Should().Be(user.Id.ToString());
        principal.FindFirst("email")!.Value.Should().Be(user.Email);
    }

    [Fact]
    public void GenerateToken_ShouldFailSignatureValidation_WhenVerifierUsesDifferentKey()
    {
        var settings = Settings();
        var token = new JwtTokenService(Options.Create(settings), new ManualTimeProvider()).GenerateToken(new("user@example.com", "hash"));
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = false,
            ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Enumerable.Repeat((byte)9, 32).ToArray())
        };

        Action act = () => new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);

        act.Should().Throw<SecurityTokenException>();
    }

    private static JwtSettings Settings() => new()
    {
        Key = Convert.ToBase64String(Enumerable.Repeat((byte)7, 32).ToArray()),
        Issuer = "test-issuer", Audience = "test-audience", ExpirationMinutes = 15
    };
}
