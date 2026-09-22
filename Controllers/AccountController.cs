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
            var response = await service.CreateAsync(request, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { response.id },
                response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AccountResponse>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Ok(await service.GetByIdAsync(id, cancellationToken));
        }


    }
}
