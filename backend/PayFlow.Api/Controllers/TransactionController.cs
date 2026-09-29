using Microsoft.AspNetCore.Mvc;
using PayFlow.Features.Transactions.DTOs;
using PayFlow.Features.Transactions;
using Microsoft.AspNetCore.Authorization;

namespace PayFlow.Controllers
{
    [Route("api/transactions")]
    [ApiController]
    [Authorize]
    public class TransactionController(TransactionService service) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<TransactionResponse>> AddTransaction(
            CreateTransactionRequest request,
            CancellationToken cancellationToken)
        {
            var result = await service.AddTransactionAsync(request, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { result.Id },
                result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TransactionResponse>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Ok(await service.GetByIdAsync(id, cancellationToken));
        }
    }
}
