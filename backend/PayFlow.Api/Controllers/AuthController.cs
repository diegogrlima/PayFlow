using Microsoft.AspNetCore.Mvc;
using PayFlow.Features.Authentication.DTOs;
using PayFlow.Features.Authentication;

namespace PayFlow.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController(AuthService service) : ControllerBase
    {
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(
            LoginRequest request,
            CancellationToken cancellationToken)
        {
            var result = await service.LoginAsync(
                request,
                cancellationToken);

            return Ok(result);
        }
    }
}
