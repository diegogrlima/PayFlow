using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PayFlow.DTOs.Account;
using PayFlow.Entities;
using PayFlow.Services;

namespace PayFlow.Controllers
{
    [Route("api/accounts")]
    [ApiController]
    public class AccountController(
        AccountService service,
        IValidator<CreateAccountRequest> validator) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<AccountResponse>> Create(
            CreateAccountRequest request,
            CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                var problemDetails = new ValidationProblemDetails(
                    validationResult.ToDictionary())
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Dados da conta inválidos."
                };

                return BadRequest(problemDetails);
            }

            var account = await service.CreateAsync(request, cancellationToken);
            var response = ToResponse(account);

            return CreatedAtAction(nameof(GetById), new { id = account.Id }, response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AccountResponse>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var account = await service.GetByIdAsync(id, cancellationToken);

            return account is null
                ? NotFound()
                : Ok(ToResponse(account));
        }

        private static AccountResponse ToResponse(Account account) =>
            new(account.Id, account.HolderName, account.Balance, account.CreatedAtUtc);
    }
}
