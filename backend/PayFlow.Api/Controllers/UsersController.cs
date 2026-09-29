using Microsoft.AspNetCore.Mvc;
using PayFlow.Features.Users.DTOs;
using PayFlow.Features.Users;
using Microsoft.AspNetCore.Authorization;

namespace PayFlow.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UsersController(UserService service) : ControllerBase
    {
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<UserResponse>> Create(
            CreateUserRequest request,
            CancellationToken cancellationToken)
        {
            var result = await service.AddUserAsync(request, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { result.Id },
                result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserResponse>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Ok(await service.GetByIdAsync(id, cancellationToken));
        }
    }
}
