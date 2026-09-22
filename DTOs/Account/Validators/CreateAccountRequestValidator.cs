using FluentValidation;

namespace PayFlow.DTOs.Account.Validators
{
    public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
    {

        public CreateAccountRequestValidator()
        {
            RuleFor(x => x.HolderName)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(120);
        }
    }
}
