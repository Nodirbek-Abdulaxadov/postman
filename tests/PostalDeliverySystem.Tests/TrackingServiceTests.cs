using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Application.Abstractions.Realtime;
using PostalDeliverySystem.Application.Abstractions.Time;
using PostalDeliverySystem.Application.Common.Exceptions;
using PostalDeliverySystem.Application.Tracking;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;
using PostalDeliverySystem.Shared.Contracts.Tracking;

namespace PostalDeliverySystem.Tests;

public sealed class TrackingServiceTests
{
    [Fact]
    public async Task IngestLocation_AcceptsAndPersists_WhenValid()
    {
        var now = new DateTimeOffset(2026, 4, 15, 18, 0, 0, TimeSpan.Zero);
        var courierId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var orderRepository = CreateOrderRepository(courierId, orderId, branchId, OrderStatus.OnTheWay);
        var trackingRepository = new InMemoryTrackingRepository();
        var cache = new InMemoryTrackingLocationCache();
        var publisher = new InMemoryTrackingRealtimePublisher();

        var service = CreateService(orderRepository, trackingRepository, cache, publisher, now);

        var response = await service.IngestLocationAsync(
            new TrackingLocationIngestRequest
            {
                CourierId = courierId,
                OrderId = orderId,
                Lat = 41.311081,
                Lng = 69.240562,
                AccuracyMeters = 7.5f,
                SpeedMps = 3.2f,
                DeviceTime = now
            },
            courierId,
            UserRole.Courier);

        Assert.True(response.Accepted);
        Assert.NotNull(response.RecordedAt);
        Assert.Single(trackingRepository.Items);
        Assert.Equal(orderId, trackingRepository.Items[0].OrderId);
        Assert.Single(publisher.LocationUpdates);
    }

    [Fact]
    public async Task IngestLocation_IgnoresSmallMovementBeforeHeartbeat()
    {
        var now = new DateTimeOffset(2026, 4, 15, 18, 0, 3, TimeSpan.Zero);
        var courierId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var orderRepository = CreateOrderRepository(courierId, orderId, branchId, OrderStatus.OnTheWay);
        var trackingRepository = new InMemoryTrackingRepository();
        var previous = new CourierLocation
        {
            Id = 1,
            CourierId = courierId,
            OrderId = orderId,
            Lat = 41.311081,
            Lng = 69.240562,
            CreatedAt = now.AddSeconds(-3)
        };
        trackingRepository.Items.Add(previous);

        var cache = new InMemoryTrackingLocationCache
        {
            LatestByCourier = previous
        };

        var service = CreateService(orderRepository, trackingRepository, cache, new InMemoryTrackingRealtimePublisher(), now);

        var response = await service.IngestLocationAsync(
            new TrackingLocationIngestRequest
            {
                CourierId = courierId,
                OrderId = orderId,
                Lat = 41.311082,
                Lng = 69.240562,
                DeviceTime = now
            },
            courierId,
            UserRole.Courier);

        Assert.False(response.Accepted);
        Assert.Equal("movement_below_threshold", response.IgnoredReason);
        Assert.Single(trackingRepository.Items);
    }

    [Fact]
    public async Task IngestLocation_AcceptsHeartbeatWithoutMovement()
    {
        var now = new DateTimeOffset(2026, 4, 15, 18, 0, 6, TimeSpan.Zero);
        var courierId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var orderRepository = CreateOrderRepository(courierId, orderId, branchId, OrderStatus.OnTheWay);
        var trackingRepository = new InMemoryTrackingRepository();
        var previous = new CourierLocation
        {
            Id = 1,
            CourierId = courierId,
            OrderId = orderId,
            Lat = 41.311081,
            Lng = 69.240562,
            CreatedAt = now.AddSeconds(-6)
        };
        trackingRepository.Items.Add(previous);

        var cache = new InMemoryTrackingLocationCache
        {
            LatestByCourier = previous
        };

        var service = CreateService(orderRepository, trackingRepository, cache, new InMemoryTrackingRealtimePublisher(), now);

        var response = await service.IngestLocationAsync(
            new TrackingLocationIngestRequest
            {
                CourierId = courierId,
                OrderId = orderId,
                Lat = 41.311081,
                Lng = 69.240562,
                DeviceTime = now
            },
            courierId,
            UserRole.Courier);

        Assert.True(response.Accepted);
        Assert.Equal(2, trackingRepository.Items.Count);
    }

    [Fact]
    public async Task IngestLocation_PersistsEvenWhenCacheAndPublisherFail()
    {
        var now = new DateTimeOffset(2026, 4, 15, 18, 10, 0, TimeSpan.Zero);
        var courierId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var orderRepository = CreateOrderRepository(courierId, orderId, branchId, OrderStatus.OnTheWay);
        var trackingRepository = new InMemoryTrackingRepository();
        var cache = new InMemoryTrackingLocationCache { ThrowOnSet = true, ThrowOnGet = true };
        var publisher = new InMemoryTrackingRealtimePublisher { ThrowOnPublish = true };

        var service = CreateService(orderRepository, trackingRepository, cache, publisher, now);

        var response = await service.IngestLocationAsync(
            new TrackingLocationIngestRequest
            {
                CourierId = courierId,
                OrderId = orderId,
                Lat = 41.312,
                Lng = 69.241,
                DeviceTime = now
            },
            courierId,
            UserRole.Courier);

        Assert.True(response.Accepted);
        Assert.Single(trackingRepository.Items);
    }

    [Fact]
    public async Task GetLatestOrderLocation_UsesRepositoryWhenCacheUnavailable()
    {
        var now = new DateTimeOffset(2026, 4, 15, 18, 20, 0, TimeSpan.Zero);
        var courierId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var orderRepository = CreateOrderRepository(courierId, orderId, branchId, OrderStatus.OnTheWay, customerId);
        var trackingRepository = new InMemoryTrackingRepository();
        trackingRepository.Items.Add(new CourierLocation
        {
            Id = 1,
            CourierId = courierId,
            OrderId = orderId,
            Lat = 41.313,
            Lng = 69.242,
            CreatedAt = now
        });

        var cache = new InMemoryTrackingLocationCache { ThrowOnGet = true };
        var service = CreateService(orderRepository, trackingRepository, cache, new InMemoryTrackingRealtimePublisher(), now);

        var response = await service.GetLatestOrderLocationAsync(
            orderId,
            customerId,
            UserRole.Customer,
            null);

        Assert.NotNull(response);
        Assert.Equal(orderId, response!.OrderId);
        Assert.Equal(branchId, response.BranchId);
    }

    [Fact]
    public async Task IngestLocation_RejectsNonCourierActor()
    {
        var now = new DateTimeOffset(2026, 4, 15, 18, 30, 0, TimeSpan.Zero);
        var courierId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var orderRepository = CreateOrderRepository(courierId, orderId, branchId, OrderStatus.OnTheWay);
        var service = CreateService(
            orderRepository,
            new InMemoryTrackingRepository(),
            new InMemoryTrackingLocationCache(),
            new InMemoryTrackingRealtimePublisher(),
            now);

        await Assert.ThrowsAsync<ApplicationForbiddenException>(async () =>
            await service.IngestLocationAsync(
                new TrackingLocationIngestRequest
                {
                    CourierId = courierId,
                    OrderId = orderId,
                    Lat = 41.31,
                    Lng = 69.24,
                    DeviceTime = now
                },
                Guid.NewGuid(),
                UserRole.Admin));
    }

    private static TrackingService CreateService(
        InMemoryOrderRepository orderRepository,
        InMemoryTrackingRepository trackingRepository,
        InMemoryTrackingLocationCache cache,
        InMemoryTrackingRealtimePublisher publisher,
        DateTimeOffset now)
    {
        var userRepository = new InMemoryUserRepository();
        var authorizationService = new TrackingAuthorizationService(orderRepository, userRepository);
        return new TrackingService(
            orderRepository,
            trackingRepository,
            cache,
            publisher,
            authorizationService,
            new FixedClock(now));
    }

    private static InMemoryOrderRepository CreateOrderRepository(
        Guid courierId,
        Guid orderId,
        Guid branchId,
        OrderStatus status,
        Guid? customerId = null)
    {
        var repository = new InMemoryOrderRepository();
        repository.Orders.Add(new Order
        {
            Id = orderId,
            OrderCode = "ORD-20260415-TRCK0001",
            CustomerId = customerId ?? Guid.NewGuid(),
            BranchId = branchId,
            CourierId = courierId,
            Status = status,
            RecipientName = "Receiver",
            RecipientPhone = "+998900001001",
            Address = "Tracking Address",
            Lat = 41.31,
            Lng = 69.24,
            AssignedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return repository;
    }

    private sealed class InMemoryOrderRepository : IOrderRepository
    {
        public List<Order> Orders { get; } = new();

        public Task AddAsync(Order order, OrderStatusHistory initialHistory, CancellationToken cancellationToken = default)
        {
            Orders.Add(order);
            return Task.CompletedTask;
        }

        public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Orders.FirstOrDefault(o => o.Id == orderId));
        }

        public Task<Order?> GetByIdForCourierAsync(Guid orderId, Guid courierId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Orders.FirstOrDefault(o => o.Id == orderId && o.CourierId == courierId));
        }

        public Task<IReadOnlyList<Order>> SearchAsync(OrderSearchFilter filter, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Order>>(Array.Empty<Order>());
        }

        public Task<IReadOnlyList<Order>> GetAssignedToCourierAsync(Guid courierId, bool includeCompleted, int limit, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Order>>(Array.Empty<Order>());
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
            return Task.FromResult(false);
        }

        public Task<IReadOnlyList<OrderStatusHistory>> GetStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<OrderStatusHistory>>(Array.Empty<OrderStatusHistory>());
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<IReadOnlyList<User>> SearchAsync(UserSearchFilter filter, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(Array.Empty<User>());
        }

        public Task<int> CountByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryTrackingRepository : ITrackingRepository
    {
        public List<CourierLocation> Items { get; } = new();

        public Task AddAsync(CourierLocation location, CancellationToken cancellationToken = default)
        {
            Items.Add(location);
            return Task.CompletedTask;
        }

        public Task<CourierLocation?> GetLatestByCourierAsync(Guid courierId, CancellationToken cancellationToken = default)
        {
            var item = Items
                .Where(x => x.CourierId == courierId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            return Task.FromResult(item);
        }

        public Task<CourierLocation?> GetLatestByOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            var item = Items
                .Where(x => x.OrderId == orderId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            return Task.FromResult(item);
        }
    }

    private sealed class InMemoryTrackingLocationCache : ITrackingLocationCache
    {
        public CourierLocation? LatestByCourier { get; set; }

        public CourierLocation? LatestByOrder { get; set; }

        public bool ThrowOnGet { get; set; }

        public bool ThrowOnSet { get; set; }

        public Task<CourierLocation?> GetLatestByCourierAsync(Guid courierId, CancellationToken cancellationToken = default)
        {
            if (ThrowOnGet)
            {
                throw new InvalidOperationException("Cache unavailable.");
            }

            return Task.FromResult(LatestByCourier);
        }

        public Task<CourierLocation?> GetLatestByOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            if (ThrowOnGet)
            {
                throw new InvalidOperationException("Cache unavailable.");
            }

            return Task.FromResult(LatestByOrder);
        }

        public Task SetLatestAsync(CourierLocation location, CancellationToken cancellationToken = default)
        {
            if (ThrowOnSet)
            {
                throw new InvalidOperationException("Cache unavailable.");
            }

            LatestByCourier = location;
            LatestByOrder = location;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryTrackingRealtimePublisher : ITrackingRealtimePublisher
    {
        public List<TrackingLocationUpdateEvent> LocationUpdates { get; } = new();

        public List<TrackingStatusUpdateEvent> StatusUpdates { get; } = new();

        public bool ThrowOnPublish { get; set; }

        public Task PublishLocationUpdateAsync(TrackingLocationUpdateEvent payload, CancellationToken cancellationToken = default)
        {
            if (ThrowOnPublish)
            {
                throw new InvalidOperationException("Realtime unavailable.");
            }

            LocationUpdates.Add(payload);
            return Task.CompletedTask;
        }

        public Task PublishStatusUpdateAsync(TrackingStatusUpdateEvent payload, CancellationToken cancellationToken = default)
        {
            if (ThrowOnPublish)
            {
                throw new InvalidOperationException("Realtime unavailable.");
            }

            StatusUpdates.Add(payload);
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
