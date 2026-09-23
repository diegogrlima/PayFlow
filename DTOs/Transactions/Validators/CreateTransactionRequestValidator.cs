using FluentValidation;

namespace PayFlow.DTOs.Transactions.Validators
{
    public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
    {
        public CreateTransactionRequestValidator()
        {
            RuleFor(x => x.SourceAccountId)
                .NotEmpty()
                .WithMessage("A conta de origem é obrigatória.");

            RuleFor(x => x.DestinationAccountId)
                .NotEmpty()
                .WithMessage("A conta de destino é obrigatória.")
                .NotEqual(x => x.SourceAccountId)
                .WithMessage("A conta de destino deve ser diferente da conta de origem.");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("O valor da transferência deve ser maior que zero.")
                .PrecisionScale(18, 2, false)
                .WithMessage("O valor da transferência deve possuir no máximo duas casas decimais.");
        }
    }
}
