using Microsoft.AspNetCore.Mvc;
using PayFlow.DTOs.Users;
using PayFlow.Services;

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
