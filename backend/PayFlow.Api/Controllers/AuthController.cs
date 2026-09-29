using Microsoft.AspNetCore.Mvc;
using PayFlow.Features.Authentication.DTOs;
using PayFlow.Features.Authentication;
using Microsoft.AspNetCore.Authorization;
using PayFlow.Features.Users;
using PayFlow.Features.Users.DTOs;

namespace PayFlow.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController(
        AuthService service,
        UserService userService) : ControllerBase
    {
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login(
            LoginRequest request,
            CancellationToken cancellationToken)
        {
            var result = await service.LoginAsync(
                request,
                cancellationToken);

            return Ok(result);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Refresh(
            RefreshTokenRequest request,
            CancellationToken cancellationToken)
        {
            var result = await service.RefreshAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpPost("revoke")]
        [AllowAnonymous]
        public async Task<IActionResult> Revoke(
            RevokeTokenRequest request,
            CancellationToken cancellationToken)
        {
            await service.RevokeAsync(request, cancellationToken);
            return NoContent();
        }

        [HttpGet("me", Name = "GetCurrentUser")]
        [Authorize]
        public async Task<ActionResult<UserResponse>> GetCurrent(
            CancellationToken cancellationToken)
        {
            return Ok(await userService.GetCurrentAsync(cancellationToken));
        }
    }
}
