using FluentValidation;

namespace PayFlow.DTOs.Account.Validators
{
    public class CreateDepositRequestValidator : AbstractValidator<CreateDepositRequest>
    {
        public CreateDepositRequestValidator()
        {
            RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("O valor do depósito deve ser maior que zero.")

            .PrecisionScale(18, 2, false)
            .WithMessage("O valor do depósito deve possuir no máximo duas casas decimais.");
        }
    }
}
