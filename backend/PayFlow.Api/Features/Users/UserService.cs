using FluentValidation;
using PayFlow.Features.Users.DTOs;
using PayFlow.Domain.Entities;
using PayFlow.Common.Exceptions;
using PayFlow.Infrastructure.Repositories.Interfaces;
using PayFlow.Features.Authentication.Interfaces;

namespace PayFlow.Features.Users;

public class UserService(
    IUserRepository repository,
    IValidator<CreateUserRequest> createUserValidator,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser)
{
    public async Task<UserResponse> AddUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        await createUserValidator.ValidateAndThrowAsync(
            request,
            cancellationToken);

        string email = request.Email.Trim();

        bool emailAlreadyExists = await repository.ExistsByEmailAsync(
            email,
            cancellationToken);

        if (emailAlreadyExists)
        {
            throw new BusinessRuleException(
                "Este e-mail já está em uso.");
        }

        string passwordHash = passwordHasher.Hash(request.Password);

        var user = new User(email, passwordHash);

        await repository.AddAsync(user, cancellationToken);

        return ToResponse(user);
    }

    public async Task<UserResponse> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        var user = await repository.GetByIdAsync(userId, cancellationToken)
            ?? throw new ResourceNotFoundException(
                "Usuário autenticado não encontrado.");

        return ToResponse(user);
    }

    private static UserResponse ToResponse(User user) =>
        new(user.Id, user.Email);
}
