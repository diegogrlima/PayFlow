using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using PayFlow.Common.Exceptions;
using PayFlow.Domain.Entities;
using PayFlow.Features.Authentication;
using PayFlow.Features.Authentication.DTOs;
using PayFlow.Features.Authentication.Interfaces;
using PayFlow.Features.Authentication.Validators;
using PayFlow.Infrastructure.Authentication;
using PayFlow.Infrastructure.Repositories.Interfaces;

namespace PayFlow.Api.Tests.Unit;

public class AuthServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Login_ShouldRejectCredentialsWithoutIssuingTokens_WhenUserIsMissingOrPasswordIsWrong(bool userExists)
    {
        var context = new Context();
        context.Users.Setup(x => x.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userExists ? context.User : null);
        context.Passwords.Setup(x => x.Verify("Wrong123!", context.User.PasswordHash)).Returns(false);

        Func<Task> act = () => context.Service.LoginAsync(new("user@example.com", "Wrong123!"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        context.Tokens.Verify(x => x.GenerateRefreshToken(), Times.Never);
        context.Refresh.Verify(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_ShouldPersistOnlyHashAndReturnTokenPair_WhenCredentialsAreValid()
    {
        var context = new Context();
        context.Users.Setup(x => x.GetByEmailAsync(context.User.Email, It.IsAny<CancellationToken>())).ReturnsAsync(context.User);
        context.Passwords.Setup(x => x.Verify("Valid123!", context.User.PasswordHash)).Returns(true);
        RefreshToken? persisted = null;
        context.Refresh.Setup(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((token, _) => persisted = token).Returns(Task.CompletedTask);

        var response = await context.Service.LoginAsync(new(context.User.Email, "Valid123!"));

        response.Should().BeEquivalentTo(new LoginResponse("access", "new-token", "Bearer", 900));
        persisted.Should().NotBeNull();
        persisted!.TokenHash.Should().Be("hash:new-token");
        persisted.UserId.Should().Be(context.User.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("unknown")]
    public async Task Refresh_ShouldRejectToken_WhenTokenIsBlankOrUnknown(string? token)
    {
        var context = new Context();

        Func<Task> act = () => context.Service.RefreshAsync(new(token!));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        context.Refresh.Verify(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_ShouldRevokeFamily_WhenRevokedTokenIsReused()
    {
        var context = new Context();
        var token = context.ExistingToken();
        token.Revoke("previous rotation", Guid.NewGuid());

        Func<Task> act = () => context.Service.RefreshAsync(new("old-token"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        context.Refresh.Verify(x => x.RevokeFamilyAsync(token.FamilyId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        context.Refresh.Verify(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_ShouldRotateTokenWithinSameFamily_WhenCurrentTokenIsActive()
    {
        var context = new Context();
        var original = context.ExistingToken();
        RefreshToken? successor = null;
        context.Refresh.Setup(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((token, _) => successor = token).Returns(Task.CompletedTask);

        var response = await context.Service.RefreshAsync(new("old-token"));

        response.RefreshToken.Should().Be("new-token");
        successor.Should().NotBeNull();
        successor!.FamilyId.Should().Be(original.FamilyId);
        original.IsActive.Should().BeFalse();
        original.ReplacedByTokenId.Should().Be(successor.Id);
    }

    [Fact]
    public async Task Refresh_ShouldRejectToken_WhenOwnerNoLongerExists()
    {
        var context = new Context();
        context.ExistingToken();
        context.Users.Setup(x => x.GetByIdAsync(context.User.Id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        Func<Task> act = () => context.Service.RefreshAsync(new("old-token"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        context.Refresh.Verify(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_ShouldRejectRotation_WhenPersistenceReportsConcurrencyConflict()
    {
        var context = new Context();
        context.ExistingToken();
        context.Refresh.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateConcurrencyException());

        Func<Task> act = () => context.Service.RefreshAsync(new("old-token"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task Revoke_ShouldRemainIdempotent_WhenCalledTwiceForSameToken()
    {
        var context = new Context();
        var token = context.ExistingToken();

        await context.Service.RevokeAsync(new("old-token"));
        var firstRevocation = token.RevokedAtUtc;
        await context.Service.RevokeAsync(new("old-token"));

        token.IsActive.Should().BeFalse();
        token.RevokedAtUtc.Should().Be(firstRevocation);
        context.Refresh.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Revoke_ShouldSucceed_WhenAnotherRequestAlreadyRevokedToken()
    {
        var context = new Context();
        var token = context.ExistingToken();
        context.Refresh.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateConcurrencyException());

        Func<Task> act = () => context.Service.RevokeAsync(new("old-token"));

        await act.Should().NotThrowAsync();
        token.IsActive.Should().BeFalse();
    }

    private sealed class Context
    {
        public User User { get; } = new("user@example.com", "stored-password-hash");
        public Mock<IUserRepository> Users { get; } = new();
        public Mock<IPasswordHasher> Passwords { get; } = new();
        public Mock<ITokenService> Tokens { get; } = new();
        public Mock<IRefreshTokenRepository> Refresh { get; } = new();
        public AuthService Service { get; }

        public Context()
        {
            Tokens.Setup(x => x.GenerateRefreshToken()).Returns("new-token");
            Tokens.Setup(x => x.HashRefreshToken(It.IsAny<string>())).Returns((string token) => "hash:" + token);
            Tokens.Setup(x => x.GenerateToken(User)).Returns("access");
            Users.Setup(x => x.GetByIdAsync(User.Id, It.IsAny<CancellationToken>())).ReturnsAsync(User);
            Service = new(Users.Object, Passwords.Object, new CreateLoginRequestValidator(), Tokens.Object,
                Refresh.Object, Options.Create(new JwtSettings { ExpirationMinutes = 15, RefreshTokenExpirationDays = 7 }));
        }

        public RefreshToken ExistingToken()
        {
            var token = new RefreshToken(User.Id, "hash:old-token", DateTime.MaxValue);
            Refresh.Setup(x => x.GetByHashAsync("hash:old-token", It.IsAny<CancellationToken>())).ReturnsAsync(token);
            return token;
        }
    }
}
