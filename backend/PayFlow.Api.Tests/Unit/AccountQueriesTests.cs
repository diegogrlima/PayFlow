using FluentAssertions;
using FluentValidation;
using Moq;
using PayFlow.Common.Exceptions;
using PayFlow.Domain.Entities;
using PayFlow.Features.Accounts;
using PayFlow.Features.Accounts.Validators;
using PayFlow.Features.Authentication.Interfaces;
using PayFlow.Infrastructure.Repositories.Interfaces;

namespace PayFlow.Api.Tests.Unit;

public class AccountQueriesTests
{
    [Theory]
    [InlineData(AccountType.Individual)]
    [InlineData(AccountType.Business)]
    public async Task Create_ShouldLinkZeroBalanceAccountToCurrentUser_WhenRequestIsValid(AccountType type)
    {
        var context = new Context();
        Account? saved = null;
        context.Accounts.Setup(x => x.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((account, _) => saved = account).Returns(Task.CompletedTask);

        var result = await context.Service.CreateAsync(new("  Owner Name  ", type));

        saved.Should().NotBeNull();
        saved.Should().BeEquivalentTo(new { UserId = context.User.Id, HolderName = "Owner Name", Balance = 0m, AccountType = type });
        result.userId.Should().Be(context.User.Id);
    }

    [Fact]
    public async Task Create_ShouldRejectWithoutSaving_WhenAuthenticatedUserNoLongerExists()
    {
        var context = new Context();
        context.Users.Setup(x => x.GetByIdAsync(context.User.Id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        Func<Task> act = () => context.Service.CreateAsync(new("Owner", AccountType.Individual));

        await act.Should().ThrowAsync<ResourceNotFoundException>();
        context.Accounts.Verify(x => x.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_ShouldRejectWithoutSaving_WhenRequestViolatesApiNameLimit()
    {
        var context = new Context();

        Func<Task> act = () => context.Service.CreateAsync(new(new string('a', 121), AccountType.Individual));

        await act.Should().ThrowAsync<ValidationException>();
        context.Accounts.Verify(x => x.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task List_ShouldUseCurrentOwnerAndMaskSensitiveKeys_WhenAccountsExist()
    {
        var context = new Context();
        var account = new Account(context.User.Id, "Owner");
        account.SetTransferKey(TransferKeyType.Cpf, "52998224725");
        context.Accounts.Setup(x => x.GetByUserIdAsync(context.User.Id, It.IsAny<CancellationToken>())).ReturnsAsync([account]);

        var result = await context.Service.GetAllAsync();

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        { id = account.Id, userId = context.User.Id, transferKey = "*******4725", hasTransferKey = true });
    }

    [Fact]
    public async Task List_ShouldReturnEmptyCollection_WhenCurrentUserHasNoAccounts()
    {
        var context = new Context();
        context.Accounts.Setup(x => x.GetByUserIdAsync(context.User.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await context.Service.GetAllAsync();

        result.Should().BeEmpty();
    }

    private sealed class Context
    {
        public User User { get; } = new("user@example.com", "hash");
        public Mock<IUserRepository> Users { get; } = new();
        public Mock<IAccountRepository> Accounts { get; } = new();
        public AccountService Service { get; }
        public Context()
        {
            var current = new Mock<ICurrentUser>();
            current.SetupGet(x => x.UserId).Returns(User.Id);
            Users.Setup(x => x.GetByIdAsync(User.Id, It.IsAny<CancellationToken>())).ReturnsAsync(User);
            Service = new(Accounts.Object, Users.Object, current.Object, new CreateAccountRequestValidator(), new CreateDepositRequestValidator());
        }
    }
}
