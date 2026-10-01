using FluentAssertions;
using PayFlow.Domain.Entities;
using PayFlow.Features.Accounts.DTOs;
using PayFlow.Features.Accounts.Validators;
using PayFlow.Features.Users.DTOs;
using PayFlow.Features.Users.Validators;
using PayFlow.Features.Authentication.DTOs;
using PayFlow.Features.Authentication.Validators;

namespace PayFlow.Api.Tests.Unit;

public class RequestValidationTests
{
    [Theory]
    [InlineData("too short", "Ab1!xyz", false)]
    [InlineData("no uppercase", "abcdefgh1!", false)]
    [InlineData("no lowercase", "ABCDEFGH1!", false)]
    [InlineData("no digit", "Abcdefgh!!", false)]
    [InlineData("no symbol", "Abcdefgh12", false)]
    [InlineData("valid composition", "Abcdefg1!", true)]
    public void Register_ShouldEnforcePasswordComposition_WhenPasswordIsProvided(string scenario, string password, bool accepted)
    {
        var validator = new CreateUserRequestValidator();

        var result = validator.Validate(new CreateUserRequest("user@example.com", password));

        result.IsValid.Should().Be(accepted, "the password scenario is {0}", scenario);
        if (!accepted) result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Password");
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(120, true)]
    [InlineData(121, false)]
    public void CreateAccount_ShouldEnforceApiNameLimit_WhenHolderNameIsProvided(int length, bool accepted)
    {
        var validator = new CreateAccountRequestValidator();

        var result = validator.Validate(new CreateAccountRequest(new string('a', length), AccountType.Individual));

        result.IsValid.Should().Be(accepted);
    }

    [Theory]
    [InlineData("0", false)]
    [InlineData("-1", false)]
    [InlineData("1.001", false)]
    [InlineData("10000000000000000", false)]
    [InlineData("9999999999999999.99", true)]
    [InlineData("0.01", true)]
    public void Deposit_ShouldEnforceStoragePrecision_WhenAmountIsProvided(string value, bool accepted)
    {
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var validator = new CreateDepositRequestValidator();

        var result = validator.Validate(new CreateDepositRequest(amount));

        result.IsValid.Should().Be(accepted);
    }

    [Theory]
    [InlineData("", "Valid123!", "Email")]
    [InlineData("invalid", "Valid123!", "Email")]
    [InlineData("user@example.com", "", "Password")]
    public void Login_ShouldRejectInvalidCredentials_WhenRequiredFieldsAreInvalid(string email, string password, string field)
    {
        var validator = new CreateLoginRequestValidator();

        var result = validator.Validate(new LoginRequest(email, password));

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(field);
    }
}
