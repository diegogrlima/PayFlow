using FluentValidation;

namespace PayFlow.DTOs.Account.Validators
{
    public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
    {

        public CreateAccountRequestValidator()
        {
            RuleFor(x => x.HolderName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                 .WithMessage("O nome do titular é obrigatório.")
                .MinimumLength(3)
                    .WithMessage("O nome deve possuir pelo menos 3 caracteres.")
                .MaximumLength(120)
                    .WithMessage("O nome deve possuir no máximo 120 caracteres.");
        }
    }
}
