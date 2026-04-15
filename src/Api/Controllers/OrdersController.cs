using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostalDeliverySystem.Api.Security;
using PostalDeliverySystem.Application.Orders;
using PostalDeliverySystem.Shared.Contracts.Orders;

namespace PostalDeliverySystem.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Roles = "SuperAdmin,Admin")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderResponse>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _orderService.CreateAsync(
            request,
            User.GetRequiredRole(),
            User.GetRequiredUserId(),
            User.GetOptionalBranchId(),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { orderId = created.Id }, created);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> Search(
        [FromQuery] OrderFilterRequest request,
        CancellationToken cancellationToken)
    {
        var items = await _orderService.SearchAsync(
            request,
            User.GetRequiredRole(),
            User.GetOptionalBranchId(),
            cancellationToken);

        return Ok(items);
    }

    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> GetById(Guid orderId, CancellationToken cancellationToken)
    {
        var item = await _orderService.GetByIdAsync(
            orderId,
            User.GetRequiredRole(),
            User.GetOptionalBranchId(),
            cancellationToken);

        return Ok(item);
    }

    [HttpPut("{orderId:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> Update(
        Guid orderId,
        [FromBody] UpdateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _orderService.UpdateAsync(
            orderId,
            request,
            User.GetRequiredRole(),
            User.GetOptionalBranchId(),
            cancellationToken);

        return Ok(updated);
    }

    [HttpPost("{orderId:guid}/assign")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> AssignCourier(
        Guid orderId,
        [FromBody] AssignCourierRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _orderService.AssignCourierAsync(
            orderId,
            request,
            User.GetRequiredRole(),
            User.GetRequiredUserId(),
            User.GetOptionalBranchId(),
            cancellationToken);

        return Ok(updated);
    }

    [HttpGet("{orderId:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderStatusHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderStatusHistoryResponse>>> GetHistory(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var items = await _orderService.GetHistoryAsync(
            orderId,
            User.GetRequiredRole(),
            User.GetOptionalBranchId(),
            cancellationToken);

        return Ok(items);
    }
}