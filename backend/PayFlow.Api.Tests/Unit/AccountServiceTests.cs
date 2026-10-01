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

public class AccountServiceTests
{
    [Fact]
    public async Task Deposit_ShouldIncreaseOwnedAccountBalance_WhenRequestIsValid()
    {
        var context = new Context();

        var balance = await context.Service.AddDepositAsync(context.Account.Id, new(0.01m));

        balance.Should().Be(10.01m);
        context.Account.Balance.Should().Be(10.01m);
    }

    [Fact]
    public async Task Deposit_ShouldRejectForeignAccount_WhenCallerIsNotOwner()
    {
        var context = new Context();

        Func<Task> act = () => context.Service.AddDepositAsync(Guid.NewGuid(), new(1m));

        await act.Should().ThrowAsync<ResourceNotFoundException>();
        context.Account.Balance.Should().Be(10m);
        context.Repository.Verify(x => x.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetTransferKey_ShouldReturnMaskedKey_WhenCpfIsRegistered()
    {
        var context = new Context();

        var response = await context.Service.SetTransferKeyAsync(context.Account.Id,
            new(TransferKeyType.Cpf, "529.982.247-25"), default);

        response.transferKey.Should().Be("*******4725");
        response.hasTransferKey.Should().BeTrue();
        context.Account.TransferKey.Should().Be("52998224725");
    }

    [Fact]
    public async Task SetTransferKey_ShouldRejectDuplicateWithoutSaving_WhenKeyBelongsToAnotherAccount()
    {
        var context = new Context();
        var other = new Account(Guid.NewGuid(), "Other");
        context.Repository.Setup(x => x.GetByTransferKeyAsync(TransferKeyType.Email, "other@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(other);

        Func<Task> act = () => context.Service.SetTransferKeyAsync(context.Account.Id,
            new(TransferKeyType.Email, "OTHER@Example.com"), default);

        await act.Should().ThrowAsync<ConflictException>();
        context.Repository.Verify(x => x.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetTransferKey_ShouldAllowExistingKey_WhenItBelongsToSameAccount()
    {
        var context = new Context();
        context.Account.SetTransferKey(TransferKeyType.Email, "own@example.com");
        context.Repository.Setup(x => x.GetByTransferKeyAsync(TransferKeyType.Email, "own@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(context.Account);

        var response = await context.Service.SetTransferKeyAsync(context.Account.Id,
            new(TransferKeyType.Email, "own@example.com"), default);

        response.transferKey.Should().Be("own@example.com");
    }

    [Fact]
    public async Task SetTransferKey_ShouldReturnValidationErrorWithoutInput_WhenKeyIsInvalid()
    {
        var context = new Context();

        Func<Task> act = () => context.Service.SetTransferKeyAsync(context.Account.Id,
            new(TransferKeyType.Cpf, "52998224726"), default);

        var error = (await act.Should().ThrowAsync<ValidationException>()).Which;
        error.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Value");
        error.ToString().Should().NotContain("52998224726");
    }

    [Fact]
    public async Task RemoveTransferKey_ShouldRejectForeignAccount_WhenCallerIsNotOwner()
    {
        var context = new Context();

        Func<Task> act = () => context.Service.RemoveTransferKeyAsync(Guid.NewGuid(), default);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
        context.Repository.Verify(x => x.UpdateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class Context
    {
        public Account Account { get; } = new(Guid.NewGuid(), "Owner", 10m);
        public Mock<IAccountRepository> Repository { get; } = new();
        public AccountService Service { get; }

        public Context()
        {
            var user = new Mock<ICurrentUser>();
            user.SetupGet(x => x.UserId).Returns(Account.UserId);
            Repository.Setup(x => x.GetByIdAndUserIdAsync(Account.Id, Account.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(Account);
            Service = new(Repository.Object, Mock.Of<IUserRepository>(), user.Object,
                new CreateAccountRequestValidator(), new CreateDepositRequestValidator());
        }
    }
}
