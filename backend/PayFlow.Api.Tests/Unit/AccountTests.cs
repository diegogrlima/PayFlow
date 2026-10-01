using FluentAssertions;
using PayFlow.Domain.Entities;

namespace PayFlow.Api.Tests.Unit;

public class AccountTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Deposit_ShouldPreserveBalance_WhenAmountIsNotPositive(int amount)
    {
        var account = new Account(Guid.NewGuid(), "Owner", 10m);

        Action act = () => account.Deposit(amount);

        act.Should().Throw<ArgumentOutOfRangeException>();
        account.Balance.Should().Be(10m);
    }

    [Fact]
    public void Deposit_ShouldAddExactAmount_WhenAmountIsPositive()
    {
        var account = new Account(Guid.NewGuid(), "Owner", 10m);

        account.Deposit(0.01m);

        account.Balance.Should().Be(10.01m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Debit_ShouldPreserveBalance_WhenAmountIsNotPositive(int amount)
    {
        var account = new Account(Guid.NewGuid(), "Owner", 10m);

        Action act = () => account.Debit(amount);

        act.Should().Throw<ArgumentOutOfRangeException>();
        account.Balance.Should().Be(10m);
    }

    [Fact]
    public void Debit_ShouldPreserveBalance_WhenBalanceIsInsufficient()
    {
        var account = new Account(Guid.NewGuid(), "Owner", 10m);

        Action act = () => account.Debit(10.01m);

        act.Should().Throw<InvalidOperationException>();
        account.Balance.Should().Be(10m);
    }

    [Fact]
    public void Debit_ShouldAllowZeroRemainingBalance_WhenAmountEqualsBalance()
    {
        var account = new Account(Guid.NewGuid(), "Owner", 10m);

        account.Debit(10m);

        account.Balance.Should().Be(0m);
    }

    [Fact]
    public void Update_ShouldPreserveState_WhenNewBalanceIsNegative()
    {
        var account = new Account(Guid.NewGuid(), "Original", 10m);

        Action act = () => account.Update("Replacement", -0.01m);

        act.Should().Throw<ArgumentOutOfRangeException>();
        account.HolderName.Should().Be("Original");
        account.Balance.Should().Be(10m);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" ab ")]
    public void Create_ShouldRejectInvalidHolderName_WhenTrimmedNameIsTooShort(string? name)
    {
        Action act = () => new Account(Guid.NewGuid(), name!);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(3)]
    [InlineData(150)]
    public void Create_ShouldAcceptAndTrimHolderName_WhenLengthIsAtDomainBoundary(int length)
    {
        var name = new string('a', length);

        var account = new Account(Guid.NewGuid(), $" {name} ");

        account.HolderName.Should().Be(name);
    }

    [Fact]
    public void Create_ShouldRejectHolderName_WhenTrimmedLengthExceeds150()
    {
        Action act = () => new Account(Guid.NewGuid(), new string('a', 151));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldRejectAccount_WhenOwnerIsMissing()
    {
        Action act = () => new Account(Guid.Empty, "Owner");

        act.Should().Throw<ArgumentException>().WithParameterName("userId");
    }

    [Fact]
    public void Create_ShouldRejectAccount_WhenInitialBalanceIsNegative()
    {
        Action act = () => new Account(Guid.NewGuid(), "Owner", -0.01m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_ShouldRejectAccount_WhenAccountTypeIsUndefined()
    {
        Action act = () => new Account(Guid.NewGuid(), "Owner", accountType: (AccountType)99);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(AccountType.Individual, TransferKeyType.Cnpj, "04252011000110")]
    [InlineData(AccountType.Business, TransferKeyType.Cpf, "52998224725")]
    public void SetTransferKey_ShouldPreserveExistingKey_WhenKeyIsIncompatibleWithAccount(
        AccountType accountType, TransferKeyType keyType, string key)
    {
        var account = new Account(Guid.NewGuid(), "Owner", accountType: accountType);
        account.SetTransferKey(TransferKeyType.Email, "original@example.com");

        Action act = () => account.SetTransferKey(keyType, key);

        act.Should().Throw<ArgumentException>();
        account.TransferKey.Should().Be("original@example.com");
        account.TransferKeyType.Should().Be(TransferKeyType.Email);
    }

    [Fact]
    public void SetTransferKey_ShouldPreserveExistingKey_WhenReplacementHasInvalidFormat()
    {
        var account = new Account(Guid.NewGuid(), "Owner");
        account.SetTransferKey(TransferKeyType.Email, "original@example.com");

        Action act = () => account.SetTransferKey(TransferKeyType.Cpf, "52998224726");

        act.Should().Throw<ArgumentException>();
        account.TransferKey.Should().Be("original@example.com");
        account.TransferKeyType.Should().Be(TransferKeyType.Email);
    }

    [Fact]
    public void RemoveTransferKey_ShouldClearBothFieldsWithoutChangingBalance_WhenCalledRepeatedly()
    {
        var account = new Account(Guid.NewGuid(), "Owner", 10m);
        account.SetTransferKey(TransferKeyType.Cpf, "52998224725");

        account.RemoveTransferKey();
        account.RemoveTransferKey();

        account.TransferKey.Should().BeNull();
        account.TransferKeyType.Should().BeNull();
        account.Balance.Should().Be(10m);
    }
}
