using PostalDeliverySystem.Domain.Enums;
using PostalDeliverySystem.Shared.Contracts.Orders;

namespace PostalDeliverySystem.Application.Orders;

public interface IOrderService
{
    Task<OrderResponse> CreateAsync(
        CreateOrderRequest request,
        UserRole actorRole,
        Guid actorUserId,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default);

    Task<OrderResponse> GetByIdAsync(
        Guid orderId,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderResponse>> SearchAsync(
        OrderFilterRequest request,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default);

    Task<OrderResponse> UpdateAsync(
        Guid orderId,
        UpdateOrderRequest request,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default);

    Task<OrderResponse> AssignCourierAsync(
        Guid orderId,
        AssignCourierRequest request,
        UserRole actorRole,
        Guid actorUserId,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderStatusHistoryResponse>> GetHistoryAsync(
        Guid orderId,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderResponse>> GetCustomerOrdersAsync(
        Guid actorUserId,
        UserRole actorRole,
        int limit,
        CancellationToken cancellationToken = default);

    Task<OrderResponse> GetByIdForCustomerAsync(
        Guid orderId,
        Guid actorUserId,
        UserRole actorRole,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderStatusHistoryResponse>> GetHistoryForCustomerAsync(
        Guid orderId,
        Guid actorUserId,
        UserRole actorRole,
        CancellationToken cancellationToken = default);
}