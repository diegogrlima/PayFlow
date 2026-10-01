using FluentAssertions;
using PayFlow.Domain.Entities;

namespace PayFlow.Api.Tests.Unit;

public class TransactionTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("1.001")]
    public void Create_ShouldRejectTransfer_WhenAmountViolatesMonetaryRules(string value)
    {
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        Action act = () => new Transaction(Guid.NewGuid(), Guid.NewGuid(), amount);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("10.00")]
    public void Create_ShouldRecordCompletedTransfer_WhenAmountIsValid(string value)
    {
        var source = Guid.NewGuid();
        var destination = Guid.NewGuid();
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        var transaction = new Transaction(source, destination, amount);

        transaction.Should().BeEquivalentTo(new
        {
            SourceAccountId = source, DestinationAccountId = destination,
            Amount = amount, Status = TransactionStatus.Completed
        });
    }

    [Fact]
    public void Create_ShouldRejectTransfer_WhenSourceAndDestinationAreSameAccount()
    {
        var accountId = Guid.NewGuid();

        Action act = () => new Transaction(accountId, accountId, 1m);

        act.Should().Throw<ArgumentException>().WithParameterName("destinationAccountId");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_ShouldRejectTransfer_WhenAnAccountIdentifierIsMissing(bool sourceMissing)
    {
        var source = sourceMissing ? Guid.Empty : Guid.NewGuid();
        var destination = sourceMissing ? Guid.NewGuid() : Guid.Empty;

        Action act = () => new Transaction(source, destination, 1m);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(sourceMissing ? "sourceAccountId" : "destinationAccountId");
    }
}
