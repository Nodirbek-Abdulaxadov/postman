using PostalDeliverySystem.Shared.Contracts.Orders;

namespace PostalDeliverySystem.Application.Courier;

public interface ICourierOrderService
{
    Task<IReadOnlyList<OrderResponse>> GetAssignedOrdersAsync(
        Guid courierUserId,
        bool includeCompleted,
        int limit,
        CancellationToken cancellationToken = default);

    Task<OrderResponse> GetOrderByIdAsync(
        Guid orderId,
        Guid courierUserId,
        CancellationToken cancellationToken = default);

    Task<OrderResponse> AcceptAsync(
        Guid orderId,
        Guid courierUserId,
        string? note,
        CancellationToken cancellationToken = default);

    Task<OrderResponse> PickUpAsync(
        Guid orderId,
        Guid courierUserId,
        string? note,
        CancellationToken cancellationToken = default);

    Task<OrderResponse> StartOnTheWayAsync(
        Guid orderId,
        Guid courierUserId,
        string? note,
        CancellationToken cancellationToken = default);

    Task<OrderResponse> DeliverAsync(
        Guid orderId,
        Guid courierUserId,
        string? note,
        CancellationToken cancellationToken = default);
}