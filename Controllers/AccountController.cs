using Microsoft.AspNetCore.Mvc;
using PayFlow.DTOs.Account;
using PayFlow.Services;

namespace PayFlow.Controllers
{
    [Route("api/accounts")]
    [ApiController]
    public class AccountController(
        AccountService service) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<AccountResponse>> Create(
            CreateAccountRequest request,
            CancellationToken cancellationToken)
        {
            var result = await service.CreateAsync(request, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { result.id },
                result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AccountResponse>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Ok(await service.GetByIdAsync(id, cancellationToken));
        }

        [HttpPost("{id:guid}/deposits")]
        public async Task<ActionResult<decimal>> AddDeposit(
            Guid id,
            CreateDepositRequest request,
            CancellationToken cancellationToken)
        {
            var result = await service.AddDepositAsync(id, request, cancellationToken);

            return Ok(result);
        }
    }
}
