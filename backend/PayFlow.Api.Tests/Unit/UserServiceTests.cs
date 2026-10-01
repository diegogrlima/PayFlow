using FluentAssertions;
using FluentValidation;
using Moq;
using PayFlow.Common.Exceptions;
using PayFlow.Domain.Entities;
using PayFlow.Features.Authentication.Interfaces;
using PayFlow.Features.Users;
using PayFlow.Features.Users.Validators;
using PayFlow.Infrastructure.Repositories.Interfaces;

namespace PayFlow.Api.Tests.Unit;

public class UserServiceTests
{
    [Fact]
    public async Task Register_ShouldRejectDuplicateEmail_WithoutHashingOrSavingPassword()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(x => x.ExistsByEmailAsync("user@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var hasher = new Mock<IPasswordHasher>();
        var service = new UserService(repository.Object, new CreateUserRequestValidator(), hasher.Object, Mock.Of<ICurrentUser>());

        Func<Task> act = () => service.AddUserAsync(new("user@example.com", "Valid123!"));

        await act.Should().ThrowAsync<BusinessRuleException>();
        hasher.Verify(x => x.Hash(It.IsAny<string>()), Times.Never);
        repository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Register_ShouldPersistPasswordHash_WhenRequestIsValid()
    {
        var repository = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash("Valid123!")).Returns("hashed-password");
        User? saved = null;
        repository.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => saved = user).Returns(Task.CompletedTask);
        var service = new UserService(repository.Object, new CreateUserRequestValidator(), hasher.Object, Mock.Of<ICurrentUser>());

        await service.AddUserAsync(new("user@example.com", "Valid123!"));

        saved.Should().NotBeNull();
        saved!.PasswordHash.Should().Be("hashed-password");
    }

    [Fact]
    public async Task Register_ShouldRejectWeakPassword_BeforePersistingUser()
    {
        var repository = new Mock<IUserRepository>();
        var service = new UserService(repository.Object, new CreateUserRequestValidator(), Mock.Of<IPasswordHasher>(), Mock.Of<ICurrentUser>());

        Func<Task> act = () => service.AddUserAsync(new("user@example.com", "weak"));

        await act.Should().ThrowAsync<ValidationException>();
        repository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrent_ShouldRejectRequest_WhenAuthenticatedUserWasDeleted()
    {
        var service = new UserService(Mock.Of<IUserRepository>(), new CreateUserRequestValidator(),
            Mock.Of<IPasswordHasher>(), Mock.Of<ICurrentUser>());

        Func<Task> act = () => service.GetCurrentAsync();

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
}
