using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostalDeliverySystem.Api.Security;
using PostalDeliverySystem.Application.Orders;
using PostalDeliverySystem.Shared.Contracts.Orders;

namespace PostalDeliverySystem.Api.Controllers;

[ApiController]
[Route("api/customer/orders")]
[Authorize(Roles = "Customer")]
public sealed class CustomerOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public CustomerOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetOwnOrders(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var items = await _orderService.GetCustomerOrdersAsync(
            User.GetRequiredUserId(),
            User.GetRequiredRole(),
            limit,
            cancellationToken);

        return Ok(items);
    }

    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> GetOwnOrderById(Guid orderId, CancellationToken cancellationToken = default)
    {
        var item = await _orderService.GetByIdForCustomerAsync(
            orderId,
            User.GetRequiredUserId(),
            User.GetRequiredRole(),
            cancellationToken);

        return Ok(item);
    }

    [HttpGet("{orderId:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderStatusHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderStatusHistoryResponse>>> GetOwnOrderHistory(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var items = await _orderService.GetHistoryForCustomerAsync(
            orderId,
            User.GetRequiredUserId(),
            User.GetRequiredRole(),
            cancellationToken);

        return Ok(items);
    }
}
