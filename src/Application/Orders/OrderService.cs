using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Application.Abstractions.Realtime;
using PostalDeliverySystem.Application.Abstractions.Time;
using PostalDeliverySystem.Application.Common.Exceptions;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;
using PostalDeliverySystem.Shared.Contracts.Orders;
using PostalDeliverySystem.Shared.Contracts.Tracking;

namespace PostalDeliverySystem.Application.Orders;

public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITrackingRealtimePublisher? _trackingRealtimePublisher;
    private readonly IClock _clock;

    public OrderService(
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

    public async Task<OrderResponse> CreateAsync(
        CreateOrderRequest request,
        UserRole actorRole,
        Guid actorUserId,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default)
    {
        EnsureAdminActor(actorRole, actorBranchId);

        var branchId = ResolveTargetBranchId(request.BranchId, actorRole, actorBranchId);
        var customer = await _userRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new ApplicationValidationException("Customer user was not found.");

        if (!customer.IsActive || customer.Role != UserRole.Customer)
        {
            throw new ApplicationValidationException("Provided customer is invalid or inactive.");
        }

        if (customer.BranchId != branchId)
        {
            throw new ApplicationForbiddenException("Customer must belong to the same branch as the order.");
        }

        var now = _clock.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderCode = GenerateOrderCode(now),
            CustomerId = request.CustomerId,
            BranchId = branchId,
            Status = OrderStatus.Created,
            RecipientName = NormalizeRequired(request.RecipientName, "Recipient name is required."),
            RecipientPhone = NormalizeRequired(request.RecipientPhone, "Recipient phone is required."),
            Address = NormalizeRequired(request.Address, "Address is required."),
            Lat = ValidateLatitude(request.Lat),
            Lng = ValidateLongitude(request.Lng),
            CreatedAt = now
        };

        var history = new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = null,
            ToStatus = OrderStatus.Created,
            ChangedByUserId = actorUserId,
            Note = NormalizeOptional(request.Note),
            CreatedAt = now
        };

        await _orderRepository.AddAsync(order, history, cancellationToken);
        return Map(order);
    }

    public async Task<OrderResponse> GetByIdAsync(
        Guid orderId,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default)
    {
        EnsureAdminActor(actorRole, actorBranchId);

        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Order was not found.");

        EnsureBranchAccess(order.BranchId, actorRole, actorBranchId);
        return Map(order);
    }

    public async Task<IReadOnlyList<OrderResponse>> SearchAsync(
        OrderFilterRequest request,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default)
    {
        EnsureAdminActor(actorRole, actorBranchId);

        var limit = Math.Clamp(request.Limit <= 0 ? 100 : request.Limit, 1, 200);
        var branchId = actorRole == UserRole.Admin
            ? actorBranchId
            : request.BranchId;

        if (actorRole == UserRole.Admin && request.BranchId.HasValue && request.BranchId != actorBranchId)
        {
            throw new ApplicationForbiddenException("Admin can query orders only in their branch.");
        }

        var items = await _orderRepository.SearchAsync(
            new OrderSearchFilter
            {
                BranchId = branchId,
                CustomerId = request.CustomerId,
                CourierId = request.CourierId,
                Status = request.Status,
                OrderCode = NormalizeOptional(request.OrderCode),
                Limit = limit
            },
            cancellationToken);

        return items.Select(Map).ToArray();
    }

    public async Task<OrderResponse> UpdateAsync(
        Guid orderId,
        UpdateOrderRequest request,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default)
    {
        EnsureAdminActor(actorRole, actorBranchId);

        var existing = await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Order was not found.");

        EnsureBranchAccess(existing.BranchId, actorRole, actorBranchId);
        if (existing.Status != OrderStatus.Created)
        {
            throw new ApplicationConflictException("Only orders in Created status can be edited.");
        }

        existing.RecipientName = NormalizeRequired(request.RecipientName, "Recipient name is required.");
        existing.RecipientPhone = NormalizeRequired(request.RecipientPhone, "Recipient phone is required.");
        existing.Address = NormalizeRequired(request.Address, "Address is required.");
        existing.Lat = ValidateLatitude(request.Lat);
        existing.Lng = ValidateLongitude(request.Lng);

        var updated = await _orderRepository.UpdateDetailsIfCreatedAsync(existing, cancellationToken);
        if (!updated)
        {
            throw new ApplicationConflictException("Order was changed concurrently. Reload and retry.");
        }

        return Map(existing);
    }

    public async Task<OrderResponse> AssignCourierAsync(
        Guid orderId,
        AssignCourierRequest request,
        UserRole actorRole,
        Guid actorUserId,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default)
    {
        EnsureAdminActor(actorRole, actorBranchId);

        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Order was not found.");

        EnsureBranchAccess(order.BranchId, actorRole, actorBranchId);
        if (order.Status != OrderStatus.Created)
        {
            throw new ApplicationConflictException("Only orders in Created status can be assigned.");
        }

        var courier = await _userRepository.GetByIdAsync(request.CourierId, cancellationToken)
            ?? throw new ApplicationValidationException("Courier user was not found.");

        if (!courier.IsActive || courier.Role != UserRole.Courier)
        {
            throw new ApplicationValidationException("Provided courier is invalid or inactive.");
        }

        if (courier.BranchId != order.BranchId)
        {
            throw new ApplicationForbiddenException("Courier must belong to the same branch as the order.");
        }

        var assigned = await _orderRepository.AssignCourierIfCreatedAsync(
            order.Id,
            courier.Id,
            actorUserId,
            NormalizeOptional(request.Note),
            _clock.UtcNow,
            cancellationToken);

        if (!assigned)
        {
            throw new ApplicationConflictException("Order was changed concurrently. Reload and retry assignment.");
        }

        var refreshed = await _orderRepository.GetByIdAsync(order.Id, cancellationToken)
            ?? throw new ApplicationNotFoundException("Order was not found after assignment.");

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
                        ChangedAt = _clock.UtcNow
                    },
                    cancellationToken);
            }
            catch
            {
                // Realtime fan-out is best-effort and must not fail durable assignment writes.
            }
        }

        return Map(refreshed);
    }

    public async Task<IReadOnlyList<OrderStatusHistoryResponse>> GetHistoryAsync(
        Guid orderId,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default)
    {
        EnsureAdminActor(actorRole, actorBranchId);

        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Order was not found.");

        EnsureBranchAccess(order.BranchId, actorRole, actorBranchId);

        var items = await _orderRepository.GetStatusHistoryAsync(orderId, cancellationToken);
        return items.Select(item => new OrderStatusHistoryResponse
        {
            Id = item.Id,
            OrderId = item.OrderId,
            FromStatus = item.FromStatus,
            ToStatus = item.ToStatus,
            ChangedByUserId = item.ChangedByUserId,
            Note = item.Note,
            CreatedAt = item.CreatedAt
        }).ToArray();
    }

    public async Task<IReadOnlyList<OrderResponse>> GetCustomerOrdersAsync(
        Guid actorUserId,
        UserRole actorRole,
        int limit,
        CancellationToken cancellationToken = default)
    {
        EnsureCustomerActor(actorRole);

        var normalizedLimit = Math.Clamp(limit <= 0 ? 50 : limit, 1, 200);
        var items = await _orderRepository.SearchAsync(
            new OrderSearchFilter
            {
                CustomerId = actorUserId,
                Limit = normalizedLimit
            },
            cancellationToken);

        return items.Select(Map).ToArray();
    }

    public async Task<OrderResponse> GetByIdForCustomerAsync(
        Guid orderId,
        Guid actorUserId,
        UserRole actorRole,
        CancellationToken cancellationToken = default)
    {
        EnsureCustomerActor(actorRole);

        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Order was not found.");

        EnsureCustomerOwnsOrder(order, actorUserId);
        return Map(order);
    }

    public async Task<IReadOnlyList<OrderStatusHistoryResponse>> GetHistoryForCustomerAsync(
        Guid orderId,
        Guid actorUserId,
        UserRole actorRole,
        CancellationToken cancellationToken = default)
    {
        EnsureCustomerActor(actorRole);

        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Order was not found.");

        EnsureCustomerOwnsOrder(order, actorUserId);

        var items = await _orderRepository.GetStatusHistoryAsync(orderId, cancellationToken);
        return items.Select(item => new OrderStatusHistoryResponse
        {
            Id = item.Id,
            OrderId = item.OrderId,
            FromStatus = item.FromStatus,
            ToStatus = item.ToStatus,
            ChangedByUserId = item.ChangedByUserId,
            Note = item.Note,
            CreatedAt = item.CreatedAt
        }).ToArray();
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

    private static string GenerateOrderCode(DateTimeOffset now)
    {
        var random = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"ORD-{now:yyyyMMdd}-{random}";
    }

    private static Guid ResolveTargetBranchId(Guid? requestedBranchId, UserRole actorRole, Guid? actorBranchId)
    {
        if (actorRole == UserRole.Admin)
        {
            if (!actorBranchId.HasValue)
            {
                throw new ApplicationForbiddenException("Admin user is missing branch scope.");
            }

            if (requestedBranchId.HasValue && requestedBranchId.Value != actorBranchId.Value)
            {
                throw new ApplicationForbiddenException("Admin can create orders only in their own branch.");
            }

            return actorBranchId.Value;
        }

        if (!requestedBranchId.HasValue)
        {
            throw new ApplicationValidationException("Branch is required.");
        }

        return requestedBranchId.Value;
    }

    private static void EnsureAdminActor(UserRole actorRole, Guid? actorBranchId)
    {
        if (actorRole != UserRole.SuperAdmin && actorRole != UserRole.Admin)
        {
            throw new ApplicationForbiddenException("Only SuperAdmin and Admin can perform order operations.");
        }

        if (actorRole == UserRole.Admin && !actorBranchId.HasValue)
        {
            throw new ApplicationForbiddenException("Admin user is missing branch scope.");
        }
    }

    private static void EnsureBranchAccess(Guid orderBranchId, UserRole actorRole, Guid? actorBranchId)
    {
        if (actorRole == UserRole.Admin && actorBranchId != orderBranchId)
        {
            throw new ApplicationForbiddenException("Admin can access orders only in their branch.");
        }
    }

    private static void EnsureCustomerActor(UserRole actorRole)
    {
        if (actorRole != UserRole.Customer)
        {
            throw new ApplicationForbiddenException("Only Customer can perform customer order operations.");
        }
    }

    private static void EnsureCustomerOwnsOrder(Order order, Guid actorUserId)
    {
        if (order.CustomerId != actorUserId)
        {
            throw new ApplicationForbiddenException("Customer can access only their own orders.");
        }
    }

    private static string NormalizeRequired(string value, string errorMessage)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ApplicationValidationException(errorMessage);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static double ValidateLatitude(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ApplicationValidationException("Latitude is invalid.");
        }

        if (value < -90 || value > 90)
        {
            throw new ApplicationValidationException("Latitude must be between -90 and 90.");
        }

        return value;
    }

    private static double ValidateLongitude(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ApplicationValidationException("Longitude is invalid.");
        }

        if (value < -180 || value > 180)
        {
            throw new ApplicationValidationException("Longitude must be between -180 and 180.");
        }

        return value;
    }
}