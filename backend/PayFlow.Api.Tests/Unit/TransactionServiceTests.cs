using FluentAssertions;
using FluentValidation;
using Moq;
using PayFlow.Common.Exceptions;
using PayFlow.Domain.Entities;
using PayFlow.Features.Authentication.Interfaces;
using PayFlow.Features.Transactions;
using PayFlow.Features.Transactions.DTOs;
using PayFlow.Features.Transactions.Validators;
using PayFlow.Infrastructure.Repositories.Interfaces;

namespace PayFlow.Api.Tests.Unit;

public class TransactionServiceTests
{
    [Fact]
    public async Task Transfer_ShouldMoveExactAmountAndRecordTransfer_WhenDestinationKeyIsValid()
    {
        var context = new Context();
        Transaction? recorded = null;
        context.Transactions.Setup(x => x.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((transaction, _) => recorded = transaction).Returns(Task.CompletedTask);

        var result = await context.Service.AddTransactionAsync(context.Source.Id,
            new(TransferKeyType.Email, " DEST@Example.com ", 25.01m), default);

        context.Source.Balance.Should().Be(74.99m);
        context.Destination.Balance.Should().Be(25.01m);
        recorded.Should().NotBeNull();
        result.Should().BeEquivalentTo(new { SourceAccountId = context.Source.Id,
            DestinationAccountId = context.Destination.Id, Amount = 25.01m, Status = "Completed" });
    }

    [Fact]
    public async Task Transfer_ShouldFailWithoutMovingMoney_WhenSourceAccountHasInsufficientBalance()
    {
        var context = new Context();

        Func<Task> act = () => context.Transfer(100.01m);

        await act.Should().ThrowAsync<BusinessRuleException>();
        context.AssertNoMoneyMoved();
    }

    [Fact]
    public async Task Transfer_ShouldAllowEntireBalance_WhenAmountEqualsAvailableBalance()
    {
        var context = new Context();

        await context.Transfer(100m);

        context.Source.Balance.Should().Be(0m);
        context.Destination.Balance.Should().Be(100m);
    }

    [Fact]
    public async Task Transfer_ShouldRejectForeignSource_WhenAuthenticatedUserDoesNotOwnIt()
    {
        var context = new Context();

        Func<Task> act = () => context.Service.AddTransactionAsync(context.Destination.Id,
            new(TransferKeyType.Email, "dest@example.com", 1m), default);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
        context.AssertNoMoneyMoved();
    }

    [Fact]
    public async Task Transfer_ShouldRejectSelfTransfer_WhenKeyResolvesToSourceAccount()
    {
        var context = new Context();
        context.Accounts.Setup(x => x.GetByTransferKeyAsync(TransferKeyType.Email, "dest@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(context.Source);

        Func<Task> act = () => context.Transfer(1m);

        await act.Should().ThrowAsync<BusinessRuleException>();
        context.AssertNoMoneyMoved();
    }

    [Fact]
    public async Task Transfer_ShouldRejectMissingDestination_WhenKeyIsNotRegistered()
    {
        var context = new Context();
        context.Accounts.Setup(x => x.GetByTransferKeyAsync(TransferKeyType.Email, "dest@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        Func<Task> act = () => context.Transfer(1m);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
        context.AssertNoMoneyMoved();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.001")]
    public async Task Transfer_ShouldRejectInvalidAmount_BeforeChangingBalances(string value)
    {
        var context = new Context();
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        Func<Task> act = () => context.Transfer(amount);

        await act.Should().ThrowAsync<ValidationException>();
        context.AssertNoMoneyMoved();
    }

    [Fact]
    public async Task Transfer_ShouldRollbackWithUncancelledToken_WhenPersistenceFailsAfterCancellation()
    {
        var context = new Context();
        using var cancellation = new CancellationTokenSource();
        var failure = new InvalidOperationException("Persistence failed");
        context.Transactions.Setup(x => x.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback(() => cancellation.Cancel()).ThrowsAsync(failure);

        Func<Task> act = () => context.Service.AddTransactionAsync(context.Source.Id,
            new(TransferKeyType.Email, "dest@example.com", 1m), cancellation.Token);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
        context.UnitOfWork.Verify(x => x.RollbackAsync(It.Is<CancellationToken>(token => !token.IsCancellationRequested)), Times.Once);
        context.UnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class Context
    {
        public Account Source { get; } = new(Guid.NewGuid(), "Source", 100m);
        public Account Destination { get; } = new(Guid.NewGuid(), "Destination");
        public Mock<IAccountRepository> Accounts { get; } = new();
        public Mock<ITransactionRepository> Transactions { get; } = new();
        public Mock<IUnitOfWork> UnitOfWork { get; } = new();
        public TransactionService Service { get; }

        public Context()
        {
            var current = new Mock<ICurrentUser>();
            current.SetupGet(x => x.UserId).Returns(Source.UserId);
            Accounts.Setup(x => x.GetByIdAndUserIdAsync(Source.Id, Source.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(Source);
            Accounts.Setup(x => x.GetByTransferKeyAsync(TransferKeyType.Email, "dest@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(Destination);
            Service = new(Transactions.Object, Accounts.Object, UnitOfWork.Object, current.Object, new CreateTransactionRequestValidator());
        }

        public Task<TransactionResponse> Transfer(decimal amount) => Service.AddTransactionAsync(Source.Id,
            new(TransferKeyType.Email, "dest@example.com", amount), default);

        public void AssertNoMoneyMoved()
        {
            Source.Balance.Should().Be(100m);
            Destination.Balance.Should().Be(0m);
            Transactions.Verify(x => x.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
