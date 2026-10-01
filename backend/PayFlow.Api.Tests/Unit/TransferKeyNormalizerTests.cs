using FluentAssertions;
using PayFlow.Domain.Entities;

namespace PayFlow.Api.Tests.Unit;

public class TransferKeyNormalizerTests
{
    [Theory]
    [InlineData(TransferKeyType.Email, "  USER@Example.com ", "user@example.com")]
    [InlineData(TransferKeyType.Cpf, "529.982.247-25", "52998224725")]
    [InlineData(TransferKeyType.Cnpj, "04.252.011/0001-10", "04252011000110")]
    [InlineData(TransferKeyType.Phone, "+55 (11) 99999-9999", "+5511999999999")]
    public void Normalize_ShouldReturnCanonicalKey_WhenInputHasAcceptedFormatting(TransferKeyType type, string input, string expected)
    {
        var result = TransferKeyNormalizer.Normalize(type, input);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(TransferKeyType.Cpf, "11111111111")]
    [InlineData(TransferKeyType.Cpf, "52998224726")]
    [InlineData(TransferKeyType.Cnpj, "04252011000111")]
    [InlineData(TransferKeyType.Email, "Name <user@example.com>")]
    [InlineData(TransferKeyType.Email, "invalid")]
    [InlineData(TransferKeyType.Phone, "11999999999")]
    [InlineData(TransferKeyType.Phone, "+5511000000000")]
    [InlineData((TransferKeyType)99, "value")]
    public void Normalize_ShouldRejectKey_WhenFormatOrCheckDigitsAreInvalid(TransferKeyType type, string input)
    {
        Action act = () => TransferKeyNormalizer.Normalize(type, input);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Normalize_ShouldRejectKey_WhenInputIsBlank(string? input)
    {
        Action act = () => TransferKeyNormalizer.Normalize(TransferKeyType.Email, input);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Normalize_ShouldRejectKey_WhenRawInputExceeds254Characters()
    {
        var input = new string(' ', 240) + "user@example.com";

        Action act = () => TransferKeyNormalizer.Normalize(TransferKeyType.Email, input);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(TransferKeyType.Cpf, "52998224725", "*******4725")]
    [InlineData(TransferKeyType.Cnpj, "04252011000110", "**********0110")]
    [InlineData(TransferKeyType.Phone, "+5511999999999", "**********9999")]
    [InlineData(TransferKeyType.Email, "user@example.com", "user@example.com")]
    public void Mask_ShouldExposeOnlyAllowedCharacters_WhenKeyIsPresent(TransferKeyType type, string input, string expected)
    {
        var result = TransferKeyNormalizer.Mask(type, input);

        result.Should().Be(expected);
    }
}
