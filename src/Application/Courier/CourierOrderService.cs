using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Application.Abstractions.Realtime;
using PostalDeliverySystem.Application.Abstractions.Time;
using PostalDeliverySystem.Application.Common.Exceptions;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;
using PostalDeliverySystem.Shared.Contracts.Orders;
using PostalDeliverySystem.Shared.Contracts.Tracking;

namespace PostalDeliverySystem.Application.Courier;

public sealed class CourierOrderService : ICourierOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITrackingRealtimePublisher? _trackingRealtimePublisher;
    private readonly IClock _clock;

    public CourierOrderService(
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        IClock clock,
        ITrackingRealtimePublisher? trackingRealtimePublisher = null)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _clock = clock;
        _trackingRealtimePublisher = trackingRealtimePublisher;
    }

    public async Task<IReadOnlyList<OrderResponse>> GetAssignedOrdersAsync(
        Guid courierUserId,
        bool includeCompleted,
        int limit,
        CancellationToken cancellationToken = default)
    {
        await EnsureCourierUserAsync(courierUserId, cancellationToken);

        var normalizedLimit = Math.Clamp(limit <= 0 ? 50 : limit, 1, 100);
        var orders = await _orderRepository.GetAssignedToCourierAsync(
            courierUserId,
            includeCompleted,
            normalizedLimit,
            cancellationToken);

        return orders.Select(Map).ToArray();
    }

    public async Task<OrderResponse> GetOrderByIdAsync(
        Guid orderId,
        Guid courierUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCourierUserAsync(courierUserId, cancellationToken);

        var order = await _orderRepository.GetByIdForCourierAsync(orderId, courierUserId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Order not found for this courier.");

        return Map(order);
    }

    public Task<OrderResponse> AcceptAsync(
        Guid orderId,
        Guid courierUserId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        return TransitionAsync(
            orderId,
            courierUserId,
            OrderStatus.Assigned,
            OrderStatus.Accepted,
            note,
            cancellationToken);
    }

    public Task<OrderResponse> PickUpAsync(
        Guid orderId,
        Guid courierUserId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        return TransitionAsync(
            orderId,
            courierUserId,
            OrderStatus.Accepted,
            OrderStatus.PickedUp,
            note,
            cancellationToken);
    }

    public Task<OrderResponse> StartOnTheWayAsync(
        Guid orderId,
        Guid courierUserId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        return TransitionAsync(
            orderId,
            courierUserId,
            OrderStatus.PickedUp,
            OrderStatus.OnTheWay,
            note,
            cancellationToken);
    }

    public Task<OrderResponse> DeliverAsync(
        Guid orderId,
        Guid courierUserId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        return TransitionAsync(
            orderId,
            courierUserId,
            OrderStatus.OnTheWay,
            OrderStatus.Delivered,
            note,
            cancellationToken);
    }

    private async Task<OrderResponse> TransitionAsync(
        Guid orderId,
        Guid courierUserId,
        OrderStatus expectedStatus,
        OrderStatus nextStatus,
        string? note,
        CancellationToken cancellationToken)
    {
        await EnsureCourierUserAsync(courierUserId, cancellationToken);

        var now = _clock.UtcNow;
        var transitioned = await _orderRepository.TransitionStatusForCourierAsync(
            orderId,
            courierUserId,
            expectedStatus,
            nextStatus,
            courierUserId,
            NormalizeOptional(note),
            now,
            cancellationToken);

        if (!transitioned)
        {
            var existing = await _orderRepository.GetByIdForCourierAsync(orderId, courierUserId, cancellationToken);
            if (existing is null)
            {
                throw new ApplicationNotFoundException("Order not found for this courier.");
            }

            if (existing.Status != expectedStatus)
            {
                throw new ApplicationConflictException(
                    $"Invalid transition. Expected {expectedStatus} but order is {existing.Status}.");
            }

            throw new ApplicationConflictException("Order transition failed due to concurrent update. Retry the action.");
        }

        var refreshed = await _orderRepository.GetByIdForCourierAsync(orderId, courierUserId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Order not found for this courier.");

        if (_trackingRealtimePublisher is not null)
        {
            try
            {
                await _trackingRealtimePublisher.PublishStatusUpdateAsync(
                    new TrackingStatusUpdateEvent
                    {
                        OrderId = refreshed.Id,
                        CourierId = refreshed.CourierId,
                        BranchId = refreshed.BranchId,
                        Status = refreshed.Status.ToString(),
                        StatusCode = (int)refreshed.Status,
                        ChangedAt = now
                    },
                    cancellationToken);
            }
            catch
            {
                // Realtime fan-out is best-effort and must not break legal status transitions.
            }
        }

        return Map(refreshed);
    }

    private async Task EnsureCourierUserAsync(Guid courierUserId, CancellationToken cancellationToken)
    {
        var courier = await _userRepository.GetByIdAsync(courierUserId, cancellationToken)
            ?? throw new ApplicationUnauthorizedException("Courier account was not found.");

        if (!courier.IsActive || courier.Role != UserRole.Courier)
        {
            throw new ApplicationForbiddenException("Courier account is inactive or role is invalid.");
        }
    }

    private static OrderResponse Map(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            OrderCode = order.OrderCode,
            CustomerId = order.CustomerId,
            BranchId = order.BranchId,
            CourierId = order.CourierId,
            Status = order.Status,
            RecipientName = order.RecipientName,
            RecipientPhone = order.RecipientPhone,
            Address = order.Address,
            Lat = order.Lat,
            Lng = order.Lng,
            AssignedAt = order.AssignedAt,
            DeliveredAt = order.DeliveredAt,
            CreatedAt = order.CreatedAt
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}