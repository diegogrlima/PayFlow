using FluentValidation;

namespace PayFlow.DTOs.Users.Validators
{
    public class CreateLoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public CreateLoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                    .WithMessage("O e-mail é obrigatório.")
                .MaximumLength(255)
                    .WithMessage("O e-mail deve possuir no máximo 255 caracteres.")
                .EmailAddress()
                    .WithMessage("O e-mail informado não é válido.");

            RuleFor(x => x.Password)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                    .WithMessage("A senha é obrigatória.")
                .MaximumLength(128)
                    .WithMessage("A senha deve possuir no máximo 128 caracteres.");
        }
    }
}
