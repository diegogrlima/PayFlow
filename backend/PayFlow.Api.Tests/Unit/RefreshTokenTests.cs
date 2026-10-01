using FluentAssertions;
using PayFlow.Domain.Entities;

namespace PayFlow.Api.Tests.Unit;

public class RefreshTokenTests
{
    [Fact]
    public void Create_ShouldStartNewFamily_WhenNoFamilyWasProvided()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTime.MaxValue);

        token.FamilyId.Should().Be(token.Id);
        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldRetainFamily_WhenTokenIsSuccessor()
    {
        var family = Guid.NewGuid();

        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTime.MaxValue, family);

        token.FamilyId.Should().Be(family);
    }

    [Fact]
    public void Create_ShouldRejectToken_WhenExpirationIsInPast()
    {
        Action act = () => new RefreshToken(Guid.NewGuid(), "hash", DateTime.MinValue);

        act.Should().Throw<ArgumentException>().WithParameterName("expiresAtUtc");
    }

    [Fact]
    public void Revoke_ShouldDeactivateTokenAndRecordSuccessor_WhenTokenIsRotated()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTime.MaxValue);
        var successor = Guid.NewGuid();

        token.Revoke("rotation", successor);

        token.IsActive.Should().BeFalse();
        token.RevokedAtUtc.Should().NotBeNull();
        token.ReplacedByTokenId.Should().Be(successor);
        token.RevocationReason.Should().Be("rotation");
    }

    [Fact]
    public void Revoke_ShouldPreserveFirstRevocation_WhenCalledAgain()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTime.MaxValue);
        var successor = Guid.NewGuid();
        token.Revoke("first", successor);
        var revokedAt = token.RevokedAtUtc;

        token.Revoke("second", Guid.NewGuid());

        token.RevokedAtUtc.Should().Be(revokedAt);
        token.RevocationReason.Should().Be("first");
        token.ReplacedByTokenId.Should().Be(successor);
    }
}
