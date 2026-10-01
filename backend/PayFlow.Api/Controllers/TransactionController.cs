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
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TransactionResponse>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Ok(await service.GetByIdAsync(id, cancellationToken));
        }
    }
}
