using Microsoft.AspNetCore.Mvc;
using PayFlow.Features.Accounts.DTOs;
using PayFlow.Features.Transactions.DTOs;
using PayFlow.Features.Accounts;
using PayFlow.Features.Transactions;
using Microsoft.AspNetCore.Authorization;

namespace PayFlow.Controllers
{
    [Route("api/accounts")]
    [ApiController]
    [Authorize]
    public class AccountController(
        AccountService service,
        TransactionService transactionService) : ControllerBase
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

        [HttpGet("{accountId:guid}/transactions")]
        public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetAllTransactions(
            Guid accountId,
            string? type,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var result = await transactionService.GetAllTransactionsAsync(
                accountId,
                type,
                page,
                pageSize,
                cancellationToken);

            return Ok(result);
        }
    }
}
