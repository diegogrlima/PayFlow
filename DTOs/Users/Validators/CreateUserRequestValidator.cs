using FluentValidation;

namespace PayFlow.DTOs.Users.Validators
{
    public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
    {
        public CreateUserRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                    .WithMessage("O e-mail é obrigatório.")
                .MaximumLength(255)
                    .WithMessage("O e-mail deve possuir no máximo 255 caracteres.")
                .EmailAddress()
                    .WithMessage("O e-mail informado não é válido.");

            RuleFor(x => x.Password)
                .NotEmpty()
                    .WithMessage("A senha é obrigatória.")
                .MinimumLength(8)
                    .WithMessage("A senha deve possuir pelo menos 8 caracteres.")
                .Matches("[A-Z]")
                    .WithMessage("A senha deve possuir pelo menos uma letra maiúscula.")
                .Matches("[a-z]")
                    .WithMessage("A senha deve possuir pelo menos uma letra minúscula.")
                .Matches("[0-9]")
                    .WithMessage("A senha deve possuir pelo menos um número.")
                .Matches("[^a-zA-Z0-9]")
                    .WithMessage("A senha deve possuir pelo menos um caractere especial.");
        }
    }
}
