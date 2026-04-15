using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PostalDeliverySystem.Api.Security;
using PostalDeliverySystem.Application.Tracking;
using PostalDeliverySystem.Shared.Contracts.Tracking;

namespace PostalDeliverySystem.Api.Controllers;

[ApiController]
[Route("api/tracking")]
[Authorize]
public sealed class TrackingController : ControllerBase
{
    private readonly ITrackingService _trackingService;

    public TrackingController(ITrackingService trackingService)
    {
        _trackingService = trackingService;
    }

    [HttpPost("location")]
    [EnableRateLimiting("tracking")]
    [Authorize(Roles = "Courier")]
    [ProducesResponseType(typeof(TrackingLocationIngestResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrackingLocationIngestResponse>> IngestLocation(
        [FromBody] TrackingLocationIngestRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _trackingService.IngestLocationAsync(
            request,
            User.GetRequiredUserId(),
            User.GetRequiredRole(),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("orders/{orderId:guid}/latest-location")]
    [ProducesResponseType(typeof(TrackingLocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<TrackingLocationResponse>> GetLatestOrderLocation(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var response = await _trackingService.GetLatestOrderLocationAsync(
            orderId,
            User.GetRequiredUserId(),
            User.GetRequiredRole(),
            User.GetOptionalBranchId(),
            cancellationToken);

        if (response is null)
        {
            return NoContent();
        }

        return Ok(response);
    }
}
