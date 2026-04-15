using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Application.Abstractions.Persistence;

public interface IOrderRepository
{
    Task AddAsync(
        Order order,
        OrderStatusHistory initialHistory,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdForCourierAsync(Guid orderId, Guid courierId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> SearchAsync(OrderSearchFilter filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetAssignedToCourierAsync(
        Guid courierId,
        bool includeCompleted,
        int limit,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateDetailsIfCreatedAsync(Order order, CancellationToken cancellationToken = default);

    Task<bool> AssignCourierIfCreatedAsync(
        Guid orderId,
        Guid courierId,
        Guid changedByUserId,
        string? note,
        DateTimeOffset assignedAt,
        CancellationToken cancellationToken = default);

    Task<bool> TransitionStatusForCourierAsync(
        Guid orderId,
        Guid courierId,
        OrderStatus expectedStatus,
        OrderStatus nextStatus,
        Guid changedByUserId,
        string? note,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderStatusHistory>> GetStatusHistoryAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
}