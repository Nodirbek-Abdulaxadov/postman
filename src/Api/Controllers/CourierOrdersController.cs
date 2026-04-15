using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostalDeliverySystem.Api.Security;
using PostalDeliverySystem.Application.Courier;
using PostalDeliverySystem.Shared.Contracts.Courier;
using PostalDeliverySystem.Shared.Contracts.Orders;

namespace PostalDeliverySystem.Api.Controllers;

[ApiController]
[Route("api/courier/orders")]
[Authorize(Roles = "Courier")]
public sealed class CourierOrdersController : ControllerBase
{
    private readonly ICourierOrderService _courierOrderService;

    public CourierOrdersController(ICourierOrderService courierOrderService)
    {
        _courierOrderService = courierOrderService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetAssignedOrders(
        [FromQuery] CourierAssignedOrderFilterRequest request,
        CancellationToken cancellationToken)
    {
        var items = await _courierOrderService.GetAssignedOrdersAsync(
            User.GetRequiredUserId(),
            request.IncludeCompleted,
            request.Limit,
            cancellationToken);

        return Ok(items);
    }

    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> GetById(Guid orderId, CancellationToken cancellationToken)
    {
        var item = await _courierOrderService.GetOrderByIdAsync(orderId, User.GetRequiredUserId(), cancellationToken);
        return Ok(item);
    }

    [HttpPost("{orderId:guid}/accept")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> Accept(
        Guid orderId,
        [FromBody] CourierOrderActionRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _courierOrderService.AcceptAsync(
            orderId,
            User.GetRequiredUserId(),
            request.Note,
            cancellationToken);

        return Ok(updated);
    }

    [HttpPost("{orderId:guid}/pickup")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> PickUp(
        Guid orderId,
        [FromBody] CourierOrderActionRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _courierOrderService.PickUpAsync(
            orderId,
            User.GetRequiredUserId(),
            request.Note,
            cancellationToken);

        return Ok(updated);
    }

    [HttpPost("{orderId:guid}/on-the-way")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> StartOnTheWay(
        Guid orderId,
        [FromBody] CourierOrderActionRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _courierOrderService.StartOnTheWayAsync(
            orderId,
            User.GetRequiredUserId(),
            request.Note,
            cancellationToken);

        return Ok(updated);
    }

    [HttpPost("{orderId:guid}/deliver")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> Deliver(
        Guid orderId,
        [FromBody] CourierOrderActionRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _courierOrderService.DeliverAsync(
            orderId,
            User.GetRequiredUserId(),
            request.Note,
            cancellationToken);

        return Ok(updated);
    }
}