using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostalDeliverySystem.Api.Security;
using PostalDeliverySystem.Application.Users;
using PostalDeliverySystem.Shared.Contracts.Users;

namespace PostalDeliverySystem.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "SuperAdmin,Admin")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var created = await _userService.CreateAsync(
            request,
            User.GetRequiredRole(),
            User.GetOptionalBranchId(),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { userId = created.Id }, created);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> Search(
        [FromQuery] UserFilterRequest request,
        CancellationToken cancellationToken)
    {
        var items = await _userService.SearchAsync(
            request,
            User.GetRequiredRole(),
            User.GetOptionalBranchId(),
            cancellationToken);

        return Ok(items);
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponse>> GetById(Guid userId, CancellationToken cancellationToken)
    {
        var item = await _userService.GetAccessibleByIdAsync(
            userId,
            User.GetRequiredRole(),
            User.GetOptionalBranchId(),
            cancellationToken);

        return Ok(item);
    }
}