using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Application.Abstractions.Time;
using PostalDeliverySystem.Application.Common.Exceptions;
using PostalDeliverySystem.Application.Orders;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;
using PostalDeliverySystem.Shared.Contracts.Orders;

namespace PostalDeliverySystem.Tests;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task CreateOrder_AdminCannotCreateOutsideOwnBranch()
    {
        var branchA = Guid.NewGuid();
        var branchB = Guid.NewGuid();

        var userRepository = new InMemoryUserRepository();
        await userRepository.AddAsync(new User
        {
            Id = Guid.NewGuid(),
            BranchId = branchA,
            Role = UserRole.Customer,
            FullName = "Customer",
            Phone = "+998900000201",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var service = BuildService(userRepository, new InMemoryOrderRepository());

        await Assert.ThrowsAsync<ApplicationForbiddenException>(async () =>
            await service.CreateAsync(
                new CreateOrderRequest
                {
                    BranchId = branchB,
                    CustomerId = userRepository.Users[0].Id,
                    RecipientName = "Receiver",
                    RecipientPhone = "+998900000202",
                    Address = "Sample",
                    Lat = 41.31,
                    Lng = 69.24
                },
                UserRole.Admin,
                Guid.NewGuid(),
                branchA));
    }

    [Fact]
    public async Task AssignCourier_CreatesAssignedHistoryRecord()
    {
        var branchId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var userRepository = new InMemoryUserRepository();
        var customerId = Guid.NewGuid();
        var courierId = Guid.NewGuid();

        await userRepository.AddAsync(new User
        {
            Id = customerId,
            BranchId = branchId,
            Role = UserRole.Customer,
            FullName = "Customer",
            Phone = "+998900000301",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await userRepository.AddAsync(new User
        {
            Id = courierId,
            BranchId = branchId,
            Role = UserRole.Courier,
            FullName = "Courier",
            Phone = "+998900000302",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var orderRepository = new InMemoryOrderRepository();
        var service = BuildService(userRepository, orderRepository);

        var created = await service.CreateAsync(
            new CreateOrderRequest
            {
                CustomerId = customerId,
                RecipientName = "Receiver",
                RecipientPhone = "+998900000303",
                Address = "Sample",
                Lat = 41.31,
                Lng = 69.24,
                Note = "created"
            },
            UserRole.Admin,
            adminId,
            branchId);

        var assigned = await service.AssignCourierAsync(
            created.Id,
            new AssignCourierRequest
            {
                CourierId = courierId,
                Note = "assigned"
            },
            UserRole.Admin,
            adminId,
            branchId);

        Assert.Equal(OrderStatus.Assigned, assigned.Status);
        Assert.Equal(courierId, assigned.CourierId);

        var history = await service.GetHistoryAsync(created.Id, UserRole.Admin, branchId);
        Assert.Contains(history, item => item.ToStatus == OrderStatus.Created);
        Assert.Contains(history, item => item.ToStatus == OrderStatus.Assigned);
    }

    [Fact]
    public async Task Customer_CanReadOwnOrdersAndHistory()
    {
        var branchId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var userRepository = new InMemoryUserRepository();
        await userRepository.AddAsync(new User
        {
            Id = customerId,
            BranchId = branchId,
            Role = UserRole.Customer,
            FullName = "Customer",
            Phone = "+998900000501",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var orderRepository = new InMemoryOrderRepository();
        await orderRepository.AddAsync(
            new Order
            {
                Id = orderId,
                OrderCode = "ORD-20260415-DDDD0001",
                CustomerId = customerId,
                BranchId = branchId,
                Status = OrderStatus.Created,
                RecipientName = "Receiver",
                RecipientPhone = "+998900000502",
                Address = "Address",
                Lat = 41.31,
                Lng = 69.24,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new OrderStatusHistory
            {
                OrderId = orderId,
                FromStatus = null,
                ToStatus = OrderStatus.Created,
                ChangedByUserId = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow
            });

        var service = BuildService(userRepository, orderRepository);

        var list = await service.GetCustomerOrdersAsync(customerId, UserRole.Customer, 50);
        var detail = await service.GetByIdForCustomerAsync(orderId, customerId, UserRole.Customer);
        var history = await service.GetHistoryForCustomerAsync(orderId, customerId, UserRole.Customer);

        Assert.Contains(list, item => item.Id == orderId);
        Assert.Equal(orderId, detail.Id);
        Assert.Contains(history, item => item.ToStatus == OrderStatus.Created);
    }

    [Fact]
    public async Task Customer_CannotReadAnotherCustomersOrder()
    {
        var branchId = Guid.NewGuid();
        var ownerCustomerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var userRepository = new InMemoryUserRepository();
        await userRepository.AddAsync(new User
        {
            Id = ownerCustomerId,
            BranchId = branchId,
            Role = UserRole.Customer,
            FullName = "Owner",
            Phone = "+998900000511",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await userRepository.AddAsync(new User
        {
            Id = otherCustomerId,
            BranchId = branchId,
            Role = UserRole.Customer,
            FullName = "Other",
            Phone = "+998900000512",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var orderRepository = new InMemoryOrderRepository();
        await orderRepository.AddAsync(
            new Order
            {
                Id = orderId,
                OrderCode = "ORD-20260415-EEEE0001",
                CustomerId = ownerCustomerId,
                BranchId = branchId,
                Status = OrderStatus.Created,
                RecipientName = "Receiver",
                RecipientPhone = "+998900000513",
                Address = "Address",
                Lat = 41.31,
                Lng = 69.24,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new OrderStatusHistory
            {
                OrderId = orderId,
                FromStatus = null,
                ToStatus = OrderStatus.Created,
                ChangedByUserId = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow
            });

        var service = BuildService(userRepository, orderRepository);

        await Assert.ThrowsAsync<ApplicationForbiddenException>(async () =>
            await service.GetByIdForCustomerAsync(orderId, otherCustomerId, UserRole.Customer));
    }

    private static OrderService BuildService(IUserRepository userRepository, IOrderRepository orderRepository)
    {
        return new OrderService(orderRepository, userRepository, new FixedClock(new DateTimeOffset(2026, 4, 15, 12, 0, 0, TimeSpan.Zero)));
    }

    private sealed class InMemoryOrderRepository : IOrderRepository
    {
        private readonly List<Order> _orders = new();
        private readonly List<OrderStatusHistory> _history = new();
        private long _historyId = 1;

        public Task AddAsync(Order order, OrderStatusHistory initialHistory, CancellationToken cancellationToken = default)
        {
            _orders.Add(Clone(order));
            initialHistory.Id = _historyId++;
            _history.Add(Clone(initialHistory));
            return Task.CompletedTask;
        }

        public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            var item = _orders.FirstOrDefault(o => o.Id == orderId);
            return Task.FromResult(item is null ? null : Clone(item));
        }

        public Task<Order?> GetByIdForCourierAsync(Guid orderId, Guid courierId, CancellationToken cancellationToken = default)
        {
            var item = _orders.FirstOrDefault(o => o.Id == orderId && o.CourierId == courierId);
            return Task.FromResult(item is null ? null : Clone(item));
        }

        public Task<IReadOnlyList<Order>> SearchAsync(OrderSearchFilter filter, CancellationToken cancellationToken = default)
        {
            IEnumerable<Order> query = _orders;

            if (filter.BranchId.HasValue)
            {
                query = query.Where(o => o.BranchId == filter.BranchId.Value);
            }

            if (filter.CustomerId.HasValue)
            {
                query = query.Where(o => o.CustomerId == filter.CustomerId.Value);
            }

            if (filter.CourierId.HasValue)
            {
                query = query.Where(o => o.CourierId == filter.CourierId.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(o => o.Status == filter.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.OrderCode))
            {
                query = query.Where(o => o.OrderCode.Contains(filter.OrderCode, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult<IReadOnlyList<Order>>(query.Take(filter.Limit).Select(Clone).ToArray());
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

            return Task.FromResult<IReadOnlyList<Order>>(query.Take(limit).Select(Clone).ToArray());
        }

        public Task<bool> UpdateDetailsIfCreatedAsync(Order order, CancellationToken cancellationToken = default)
        {
            var existing = _orders.FirstOrDefault(o => o.Id == order.Id);
            if (existing is null || existing.Status != OrderStatus.Created)
            {
                return Task.FromResult(false);
            }

            existing.RecipientName = order.RecipientName;
            existing.RecipientPhone = order.RecipientPhone;
            existing.Address = order.Address;
            existing.Lat = order.Lat;
            existing.Lng = order.Lng;

            return Task.FromResult(true);
        }

        public Task<bool> AssignCourierIfCreatedAsync(
            Guid orderId,
            Guid courierId,
            Guid changedByUserId,
            string? note,
            DateTimeOffset assignedAt,
            CancellationToken cancellationToken = default)
        {
            var existing = _orders.FirstOrDefault(o => o.Id == orderId);
            if (existing is null || existing.Status != OrderStatus.Created)
            {
                return Task.FromResult(false);
            }

            existing.CourierId = courierId;
            existing.Status = OrderStatus.Assigned;
            existing.AssignedAt = assignedAt;

            _history.Add(new OrderStatusHistory
            {
                Id = _historyId++,
                OrderId = orderId,
                FromStatus = OrderStatus.Created,
                ToStatus = OrderStatus.Assigned,
                ChangedByUserId = changedByUserId,
                Note = note,
                CreatedAt = assignedAt
            });

            return Task.FromResult(true);
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
            var items = _history.Where(h => h.OrderId == orderId).OrderByDescending(h => h.CreatedAt).Select(Clone).ToArray();
            return Task.FromResult<IReadOnlyList<OrderStatusHistory>>(items);
        }

        private static Order Clone(Order item)
        {
            return new Order
            {
                Id = item.Id,
                OrderCode = item.OrderCode,
                CustomerId = item.CustomerId,
                BranchId = item.BranchId,
                CourierId = item.CourierId,
                Status = item.Status,
                RecipientName = item.RecipientName,
                RecipientPhone = item.RecipientPhone,
                Address = item.Address,
                Lat = item.Lat,
                Lng = item.Lng,
                AssignedAt = item.AssignedAt,
                DeliveredAt = item.DeliveredAt,
                CreatedAt = item.CreatedAt
            };
        }

        private static OrderStatusHistory Clone(OrderStatusHistory item)
        {
            return new OrderStatusHistory
            {
                Id = item.Id,
                OrderId = item.OrderId,
                FromStatus = item.FromStatus,
                ToStatus = item.ToStatus,
                ChangedByUserId = item.ChangedByUserId,
                Note = item.Note,
                CreatedAt = item.CreatedAt
            };
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly List<User> _users = new();

        public IReadOnlyList<User> Users => _users;

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
            IEnumerable<User> query = _users;

            if (filter.BranchId.HasValue)
            {
                query = query.Where(u => u.BranchId == filter.BranchId.Value);
            }

            if (filter.Role.HasValue)
            {
                query = query.Where(u => u.Role == filter.Role.Value);
            }

            return Task.FromResult<IReadOnlyList<User>>(query.Take(filter.Limit).ToArray());
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
