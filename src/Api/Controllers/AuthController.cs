using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PostalDeliverySystem.Api.Security;
using PostalDeliverySystem.Application.Auth;
using PostalDeliverySystem.Shared.Contracts.Auth;

namespace PostalDeliverySystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("bootstrap-superadmin")]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthTokensResponse>> BootstrapSuperAdmin(
        [FromBody] BootstrapSuperAdminRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.BootstrapSuperAdminAsync(request, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthTokensResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthTokensResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshAsync(request, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        await _authService.RevokeRefreshTokenAsync(request, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    public ActionResult<CurrentUserResponse> Me()
    {
        return Ok(new CurrentUserResponse
        {
            Id = User.GetRequiredUserId(),
            BranchId = User.GetOptionalBranchId(),
            Role = User.GetRequiredRole(),
            FullName = User.GetRequiredFullName(),
            Phone = User.GetRequiredPhone()
        });
    }
}