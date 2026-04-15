using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostalDeliverySystem.Api.Security;
using PostalDeliverySystem.Application.Branches;
using PostalDeliverySystem.Shared.Contracts.Branches;

namespace PostalDeliverySystem.Api.Controllers;

[ApiController]
[Route("api/branches")]
[Authorize(Roles = "SuperAdmin,Admin")]
public sealed class BranchesController : ControllerBase
{
    private readonly IBranchService _branchService;

    public BranchesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BranchResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BranchResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _branchService.GetAccessibleBranchesAsync(
            User.GetRequiredRole(),
            User.GetOptionalBranchId(),
            cancellationToken);

        return Ok(items);
    }

    [HttpGet("{branchId:guid}")]
    [ProducesResponseType(typeof(BranchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BranchResponse>> GetById(Guid branchId, CancellationToken cancellationToken)
    {
        var item = await _branchService.GetAccessibleByIdAsync(
            branchId,
            User.GetRequiredRole(),
            User.GetOptionalBranchId(),
            cancellationToken);

        return Ok(item);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost]
    [ProducesResponseType(typeof(BranchResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<BranchResponse>> Create([FromBody] CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var created = await _branchService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { branchId = created.Id }, created);
    }
}