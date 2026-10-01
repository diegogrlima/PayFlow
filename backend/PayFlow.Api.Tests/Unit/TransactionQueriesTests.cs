using FluentAssertions;
using Moq;
using PayFlow.Common.Exceptions;
using PayFlow.Domain.Entities;
using PayFlow.Features.Authentication.Interfaces;
using PayFlow.Features.Transactions;
using PayFlow.Features.Transactions.Validators;
using PayFlow.Infrastructure.Repositories.Interfaces;

namespace PayFlow.Api.Tests.Unit;

public class TransactionQueriesTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetById_ShouldAllowParticipant_WhenCallerOwnsSourceOrDestination(bool ownsSource)
    {
        var context = new Context();
        var transaction = new Transaction(Guid.NewGuid(), Guid.NewGuid(), 2m);
        context.Transactions.Setup(x => x.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        var ownedId = ownsSource ? transaction.SourceAccountId : transaction.DestinationAccountId;
        context.Accounts.Setup(x => x.GetByIdAndUserIdAsync(ownedId, context.Owner.Id, It.IsAny<CancellationToken>())).ReturnsAsync(context.Account);

        var result = await context.Service.GetByIdAsync(transaction.Id);

        result.Id.Should().Be(transaction.Id);
    }

    [Fact]
    public async Task GetById_ShouldHideTransaction_WhenCallerOwnsNeitherAccount()
    {
        var context = new Context();
        var transaction = new Transaction(Guid.NewGuid(), Guid.NewGuid(), 2m);
        context.Transactions.Setup(x => x.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(transaction);

        Func<Task> act = () => context.Service.GetByIdAsync(transaction.Id);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task History_ShouldRejectForeignAccount_WithoutReadingTransactions()
    {
        var context = new Context();

        Func<Task> act = () => context.Service.GetAllTransactionsAsync(Guid.NewGuid(), null);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
        context.Transactions.Verify(x => x.GetHistoryByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<string?>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0, 9, 1, 10)]
    [InlineData(-1, 101, 1, 10)]
    [InlineData(2, 10, 2, 10)]
    [InlineData(3, 100, 3, 100)]
    public async Task History_ShouldApplyPaginationPolicy_WhenPageOrSizeIsProvided(int page, int size, int expectedPage, int expectedSize)
    {
        var context = new Context();
        var transaction = new Transaction(context.Account.Id, Guid.NewGuid(), 2m);
        context.Transactions.Setup(x => x.GetHistoryByAccountIdAsync(context.Account.Id, "sent", expectedPage, expectedSize,
            It.IsAny<CancellationToken>())).ReturnsAsync([transaction]);

        var result = await context.Service.GetAllTransactionsAsync(context.Account.Id, "sent", page, size);

        result.Should().ContainSingle().Which.Id.Should().Be(transaction.Id);
    }

    private sealed class Context
    {
        public User Owner { get; } = new("owner@example.com", "hash");
        public Account Account { get; }
        public Mock<IAccountRepository> Accounts { get; } = new();
        public Mock<ITransactionRepository> Transactions { get; } = new();
        public TransactionService Service { get; }
        public Context()
        {
            Account = new(Owner.Id, "Owner");
            var current = new Mock<ICurrentUser>();
            current.SetupGet(x => x.UserId).Returns(Owner.Id);
            Accounts.Setup(x => x.GetByIdAndUserIdAsync(Account.Id, Owner.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Account);
            Service = new(Transactions.Object, Accounts.Object, Mock.Of<IUnitOfWork>(), current.Object, new CreateTransactionRequestValidator());
        }
    }
}
