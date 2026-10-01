using FluentValidation;
using PayFlow.Features.Transactions.DTOs;

namespace PayFlow.Features.Transactions.Validators
{
    public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
    {
        public CreateTransactionRequestValidator()
        {
            RuleFor(x => x.DestinationKeyType).IsInEnum();
            RuleFor(x => x.DestinationKey).NotEmpty().MaximumLength(254);

            RuleFor(x => x.Amount)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithMessage("O valor da transferência deve ser maior que zero.")
                .PrecisionScale(18, 2, false)
                .WithMessage("O valor da transferência deve possuir no máximo duas casas decimais.");
        }
    }
}
