using FluentValidation;
using PayFlow.DTOs.Users;
using PayFlow.Entities;
using PayFlow.Exceptions;
using PayFlow.Repositories.Interfaces;
using PayFlow.Services.Interfaces;

namespace PayFlow.Services;

public class UserService(
    IUserRepository repository,
    IValidator<CreateUserRequest> createUserValidator,
    IPasswordHasher passwordHasher)
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

    public async Task<UserResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException(
                $"Usuário '{id}' não encontrado.");

        return ToResponse(user);
    }

    private static UserResponse ToResponse(User user) =>
        new(user.Id, user.Email);
}
