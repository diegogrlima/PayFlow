using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using PayFlow.Common.Exceptions;
using PayFlow.Domain.Entities;
using PayFlow.Features.Authentication;
using PayFlow.Features.Authentication.Interfaces;
using PayFlow.Features.Authentication.Validators;
using PayFlow.Infrastructure.Authentication;
using PayFlow.Infrastructure.Repositories.Interfaces;

namespace PayFlow.Api.Tests.Unit;

public class TokenExpirationTests
{
    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void IsActive_ShouldRespectExactExpirationBoundary_WhenClockAdvances(int secondsFromExpiration, bool active)
    {
        var clock = new ManualTimeProvider();
        var token = new RefreshToken(Guid.NewGuid(), "hash", clock.GetUtcNow().UtcDateTime.AddMinutes(1), timeProvider: clock);

        clock.Advance(TimeSpan.FromSeconds(60 + secondsFromExpiration));

        token.IsActive.Should().Be(active);
    }

    [Fact]
    public void Create_ShouldRejectExpirationEqualToCurrentTime_WhenClockIsControlled()
    {
        var clock = new ManualTimeProvider();

        Action act = () => new RefreshToken(Guid.NewGuid(), "hash", clock.GetUtcNow().UtcDateTime, timeProvider: clock);

        act.Should().Throw<ArgumentException>().WithParameterName("expiresAtUtc");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Refresh_ShouldRevokeAndRejectToken_WhenExpirationIsReached(int secondsAfterExpiration)
    {
        var clock = new ManualTimeProvider();
        var token = new RefreshToken(Guid.NewGuid(), "hash", clock.GetUtcNow().UtcDateTime.AddMinutes(1), timeProvider: clock);
        var tokens = new Mock<ITokenService>();
        tokens.Setup(x => x.HashRefreshToken("plain-token")).Returns("hash");
        var repository = new Mock<IRefreshTokenRepository>();
        repository.Setup(x => x.GetByHashAsync("hash", It.IsAny<CancellationToken>())).ReturnsAsync(token);
        var service = new AuthService(Mock.Of<IUserRepository>(), Mock.Of<IPasswordHasher>(),
            new CreateLoginRequestValidator(), tokens.Object, repository.Object, Options.Create(new JwtSettings()), clock);
        clock.Advance(TimeSpan.FromSeconds(60 + secondsAfterExpiration));

        Func<Task> act = () => service.RefreshAsync(new("plain-token"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        token.RevokedAtUtc.Should().Be(clock.GetUtcNow().UtcDateTime);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_ShouldIssueSuccessorWithConfiguredLifetime_WhenTokenIsJustBeforeExpiration()
    {
        var clock = new ManualTimeProvider();
        var user = new User("user@example.com", "hash");
        var original = new RefreshToken(user.Id, "old-hash", clock.GetUtcNow().UtcDateTime.AddMinutes(1), timeProvider: clock);
        var tokens = new Mock<ITokenService>();
        tokens.Setup(x => x.HashRefreshToken("old-token")).Returns("old-hash");
        tokens.Setup(x => x.GenerateRefreshToken()).Returns("successor");
        tokens.Setup(x => x.HashRefreshToken("successor")).Returns("new-hash");
        var repository = new Mock<IRefreshTokenRepository>();
        repository.Setup(x => x.GetByHashAsync("old-hash", It.IsAny<CancellationToken>())).ReturnsAsync(original);
        RefreshToken? successor = null;
        repository.Setup(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((value, _) => successor = value).Returns(Task.CompletedTask);
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var service = new AuthService(users.Object, Mock.Of<IPasswordHasher>(), new CreateLoginRequestValidator(),
            tokens.Object, repository.Object, Options.Create(new JwtSettings { RefreshTokenExpirationDays = 2 }), clock);
        clock.Advance(TimeSpan.FromSeconds(59));

        await service.RefreshAsync(new("old-token"));

        successor.Should().NotBeNull();
        successor!.ExpiresAtUtc.Should().Be(clock.GetUtcNow().UtcDateTime.AddDays(2));
    }
}
