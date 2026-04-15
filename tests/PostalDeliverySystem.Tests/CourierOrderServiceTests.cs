using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Application.Abstractions.Time;
using PostalDeliverySystem.Application.Common.Exceptions;
using PostalDeliverySystem.Application.Courier;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Tests;

public sealed class CourierOrderServiceTests
{
    [Fact]
    public async Task Courier_CanCompleteLegalLifecycle()
    {
        var now = new DateTimeOffset(2026, 4, 15, 14, 0, 0, TimeSpan.Zero);
        var courierId = Guid.NewGuid();

        var userRepository = new InMemoryUserRepository();
        await userRepository.AddAsync(new User
        {
            Id = courierId,
            Role = UserRole.Courier,
            BranchId = Guid.NewGuid(),
            FullName = "Courier",
            Phone = "+998900000401",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = now
        });

        var orderRepository = new InMemoryOrderRepository();
        var orderId = Guid.NewGuid();

        await orderRepository.AddAsync(
            new Order
            {
                Id = orderId,
                OrderCode = "ORD-20260415-AAAA0001",
                CustomerId = Guid.NewGuid(),
                BranchId = Guid.NewGuid(),
                CourierId = courierId,
                Status = OrderStatus.Assigned,
                RecipientName = "Receiver",
                RecipientPhone = "+998900000402",
                Address = "Address",
                Lat = 41.31,
                Lng = 69.24,
                AssignedAt = now,
                CreatedAt = now
            },
            new OrderStatusHistory
            {
                OrderId = orderId,
                FromStatus = OrderStatus.Created,
                ToStatus = OrderStatus.Assigned,
                ChangedByUserId = Guid.NewGuid(),
                CreatedAt = now
            });

        var service = new CourierOrderService(orderRepository, userRepository, new FixedClock(now));

        var accepted = await service.AcceptAsync(orderId, courierId, "accepted");
        Assert.Equal(OrderStatus.Accepted, accepted.Status);

        var pickedUp = await service.PickUpAsync(orderId, courierId, "picked up");
        Assert.Equal(OrderStatus.PickedUp, pickedUp.Status);

        var onTheWay = await service.StartOnTheWayAsync(orderId, courierId, "on route");
        Assert.Equal(OrderStatus.OnTheWay, onTheWay.Status);

        var delivered = await service.DeliverAsync(orderId, courierId, "delivered");
        Assert.Equal(OrderStatus.Delivered, delivered.Status);
        Assert.NotNull(delivered.DeliveredAt);

        var history = await orderRepository.GetStatusHistoryAsync(orderId);
        Assert.Contains(history, h => h.ToStatus == OrderStatus.Accepted);
        Assert.Contains(history, h => h.ToStatus == OrderStatus.PickedUp);
        Assert.Contains(history, h => h.ToStatus == OrderStatus.OnTheWay);
        Assert.Contains(history, h => h.ToStatus == OrderStatus.Delivered);
    }

    [Fact]
    public async Task Courier_CannotTransitionUnassignedOrder()
    {
        var courierId = Guid.NewGuid();
        var otherCourierId = Guid.NewGuid();

        var userRepository = new InMemoryUserRepository();
        await userRepository.AddAsync(new User
        {
            Id = courierId,
            Role = UserRole.Courier,
            BranchId = Guid.NewGuid(),
            FullName = "Courier",
            Phone = "+998900000411",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var orderRepository = new InMemoryOrderRepository();
        var orderId = Guid.NewGuid();
        await orderRepository.AddAsync(
            new Order
            {
                Id = orderId,
                OrderCode = "ORD-20260415-BBBB0001",
                CustomerId = Guid.NewGuid(),
                BranchId = Guid.NewGuid(),
                CourierId = otherCourierId,
                Status = OrderStatus.Assigned,
                RecipientName = "Receiver",
                RecipientPhone = "+998900000412",
                Address = "Address",
                Lat = 41.31,
                Lng = 69.24,
                AssignedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new OrderStatusHistory
            {
                OrderId = orderId,
                FromStatus = OrderStatus.Created,
                ToStatus = OrderStatus.Assigned,
                ChangedByUserId = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow
            });

        var service = new CourierOrderService(orderRepository, userRepository, new FixedClock(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<ApplicationNotFoundException>(async () =>
            await service.AcceptAsync(orderId, courierId, null));
    }

    [Fact]
    public async Task Courier_CannotSkipStatuses()
    {
        var courierId = Guid.NewGuid();

        var userRepository = new InMemoryUserRepository();
        await userRepository.AddAsync(new User
        {
            Id = courierId,
            Role = UserRole.Courier,
            BranchId = Guid.NewGuid(),
            FullName = "Courier",
            Phone = "+998900000421",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var orderRepository = new InMemoryOrderRepository();
        var orderId = Guid.NewGuid();
        await orderRepository.AddAsync(
            new Order
            {
                Id = orderId,
                OrderCode = "ORD-20260415-CCCC0001",
                CustomerId = Guid.NewGuid(),
                BranchId = Guid.NewGuid(),
                CourierId = courierId,
                Status = OrderStatus.Assigned,
                RecipientName = "Receiver",
                RecipientPhone = "+998900000422",
                Address = "Address",
                Lat = 41.31,
                Lng = 69.24,
                AssignedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new OrderStatusHistory
            {
                OrderId = orderId,
                FromStatus = OrderStatus.Created,
                ToStatus = OrderStatus.Assigned,
                ChangedByUserId = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow
            });

        var service = new CourierOrderService(orderRepository, userRepository, new FixedClock(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<ApplicationConflictException>(async () =>
            await service.PickUpAsync(orderId, courierId, null));
    }

    private sealed class InMemoryOrderRepository : IOrderRepository
    {
        private readonly List<Order> _orders = new();
        private readonly List<OrderStatusHistory> _history = new();
        private long _historyId = 1;

        public Task AddAsync(Order order, OrderStatusHistory initialHistory, CancellationToken cancellationToken = default)
        {
            _orders.Add(CloneOrder(order));
            initialHistory.Id = _historyId++;
            _history.Add(CloneHistory(initialHistory));
            return Task.CompletedTask;
        }

        public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            var item = _orders.FirstOrDefault(o => o.Id == orderId);
            return Task.FromResult(item is null ? null : CloneOrder(item));
        }

        public Task<Order?> GetByIdForCourierAsync(Guid orderId, Guid courierId, CancellationToken cancellationToken = default)
        {
            var item = _orders.FirstOrDefault(o => o.Id == orderId && o.CourierId == courierId);
            return Task.FromResult(item is null ? null : CloneOrder(item));
        }

        public Task<IReadOnlyList<Order>> SearchAsync(OrderSearchFilter filter, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Order>>(Array.Empty<Order>());
        }

        public Task<IReadOnlyList<Order>> GetAssignedToCourierAsync(
            Guid courierId,
            bool includeCompleted,
            int limit,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Order> query = _orders.Where(o => o.CourierId == courierId);
            if (!includeCompleted)
            {
                query = query.Where(o =>
                    o.Status == OrderStatus.Assigned ||
                    o.Status == OrderStatus.Accepted ||
                    o.Status == OrderStatus.PickedUp ||
                    o.Status == OrderStatus.OnTheWay);
            }

            return Task.FromResult<IReadOnlyList<Order>>(query.Take(limit).Select(CloneOrder).ToArray());
        }

        public Task<bool> UpdateDetailsIfCreatedAsync(Order order, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> AssignCourierIfCreatedAsync(
            Guid orderId,
            Guid courierId,
            Guid changedByUserId,
            string? note,
            DateTimeOffset assignedAt,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> TransitionStatusForCourierAsync(
            Guid orderId,
            Guid courierId,
            OrderStatus expectedStatus,
            OrderStatus nextStatus,
            Guid changedByUserId,
            string? note,
            DateTimeOffset changedAt,
            CancellationToken cancellationToken = default)
        {
            var existing = _orders.FirstOrDefault(o => o.Id == orderId && o.CourierId == courierId);
            if (existing is null || existing.Status != expectedStatus)
            {
                return Task.FromResult(false);
            }

            existing.Status = nextStatus;
            if (nextStatus == OrderStatus.Delivered)
            {
                existing.DeliveredAt = changedAt;
            }

            _history.Add(new OrderStatusHistory
            {
                Id = _historyId++,
                OrderId = orderId,
                FromStatus = expectedStatus,
                ToStatus = nextStatus,
                ChangedByUserId = changedByUserId,
                Note = note,
                CreatedAt = changedAt
            });

            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<OrderStatusHistory>> GetStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            var items = _history
                .Where(h => h.OrderId == orderId)
                .OrderByDescending(h => h.CreatedAt)
                .Select(CloneHistory)
                .ToArray();

            return Task.FromResult<IReadOnlyList<OrderStatusHistory>>(items);
        }

        private static Order CloneOrder(Order order)
        {
            return new Order
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

        private static OrderStatusHistory CloneHistory(OrderStatusHistory history)
        {
            return new OrderStatusHistory
            {
                Id = history.Id,
                OrderId = history.OrderId,
                FromStatus = history.FromStatus,
                ToStatus = history.ToStatus,
                ChangedByUserId = history.ChangedByUserId,
                Note = history.Note,
                CreatedAt = history.CreatedAt
            };
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly List<User> _users = new();

        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Id == userId));
        }

        public Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Phone == phone));
        }

        public Task<IReadOnlyList<User>> SearchAsync(UserSearchFilter filter, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(Array.Empty<User>());
        }

        public Task<int> CountByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.Count(u => u.Role == role));
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}
