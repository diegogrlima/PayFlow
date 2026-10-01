using FluentAssertions;
using PayFlow.Infrastructure.Authentication;

namespace PayFlow.Api.Tests.Unit;

public class PasswordHasherTests
{
    [Fact]
    public void Verify_ShouldAcceptOriginalPassword_WhenHashWasCreatedByHasher()
    {
        var hasher = new Argon2PasswordHasher();
        var hash = hasher.Hash("Valid123!");

        var accepted = hasher.Verify("Valid123!", hash);

        accepted.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldRejectDifferentPassword_WhenHashIsValid()
    {
        var hasher = new Argon2PasswordHasher();
        var hash = hasher.Hash("Valid123!");

        var accepted = hasher.Verify("Different123!", hash);

        accepted.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("19456$2$1$invalid-base64$invalid-base64")]
    [InlineData("1$2$1$YWJj$YWJj")]
    [InlineData("19456$2$1$YWJj$YWJj")]
    public void Verify_ShouldRejectMalformedHashWithoutThrowing_WhenStoredHashIsInvalid(string hash)
    {
        var hasher = new Argon2PasswordHasher();

        var accepted = hasher.Verify("Valid123!", hash);

        accepted.Should().BeFalse();
    }
}
