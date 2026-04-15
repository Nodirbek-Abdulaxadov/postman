using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Application.Abstractions.Time;
using PostalDeliverySystem.Application.Workers;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Tests;

public sealed class WorkerTests
{
    // ──────────────────────────────────────────────
    // RefreshTokenCleanupWorker
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Cleanup_DeletesExpiredTokens()
    {
        var now = new DateTimeOffset(2026, 4, 15, 12, 0, 0, TimeSpan.Zero);
        var repo = new InMemoryRefreshTokenRepository();

        // Expired 2 days ago
        await repo.AddAsync(new RefreshToken
        {
            UserId = Guid.NewGuid(),
            TokenHash = "expired-hash-1",
            ExpiresAt = now.AddDays(-2),
            CreatedAt = now.AddDays(-32)
        });

        // Valid token (expires in 20 days)
        await repo.AddAsync(new RefreshToken
        {
            UserId = Guid.NewGuid(),
            TokenHash = "valid-hash-1",
            ExpiresAt = now.AddDays(20),
            CreatedAt = now.AddDays(-5)
        });

        var deleted = await repo.DeleteExpiredAsync(now, now.AddDays(-7));

        Assert.Equal(1, deleted);
    }

    [Fact]
    public async Task Cleanup_DeletesOldRevokedTokens()
    {
        var now = new DateTimeOffset(2026, 4, 15, 12, 0, 0, TimeSpan.Zero);
        var repo = new InMemoryRefreshTokenRepository();

        // Revoked 10 days ago (outside retention window)
        await repo.AddAsync(new RefreshToken
        {
            UserId = Guid.NewGuid(),
            TokenHash = "revoked-old-hash",
            ExpiresAt = now.AddDays(5),
            RevokedAt = now.AddDays(-10),
            CreatedAt = now.AddDays(-15)
        });

        // Revoked 2 days ago (within retention window)
        await repo.AddAsync(new RefreshToken
        {
            UserId = Guid.NewGuid(),
            TokenHash = "revoked-recent-hash",
            ExpiresAt = now.AddDays(5),
            RevokedAt = now.AddDays(-2),
            CreatedAt = now.AddDays(-5)
        });

        var revokedBefore = now.AddDays(-7);
        var deleted = await repo.DeleteExpiredAsync(now, revokedBefore);

        Assert.Equal(1, deleted);
    }

    [Fact]
    public async Task Cleanup_LeavesValidActiveTokensAlone()
    {
        var now = new DateTimeOffset(2026, 4, 15, 12, 0, 0, TimeSpan.Zero);
        var repo = new InMemoryRefreshTokenRepository();

        await repo.AddAsync(new RefreshToken
        {
            UserId = Guid.NewGuid(),
            TokenHash = "active-token",
            ExpiresAt = now.AddDays(30),
            CreatedAt = now
        });

        var deleted = await repo.DeleteExpiredAsync(now, now.AddDays(-7));

        Assert.Equal(0, deleted);
    }

    // ──────────────────────────────────────────────
    // StaleCourierDetectorWorker (log assertion via fake logger)
    // ──────────────────────────────────────────────

    [Fact]
    public async Task StaleCourierDetector_LogsWarning_WhenCourierHasNoRecentLocation()
    {
        var now = new DateTimeOffset(2026, 4, 15, 18, 0, 0, TimeSpan.Zero);
        var courierId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var orderRepository = new InMemoryOrderRepositoryForWorker();
        orderRepository.Orders.Add(new Order
        {
            Id = orderId,
            OrderCode = "ORD-STALE-0001",
            CustomerId = Guid.NewGuid(),
            BranchId = branchId,
            CourierId = courierId,
            Status = OrderStatus.OnTheWay,
            RecipientName = "Test",
            RecipientPhone = "+998900000001",
            Address = "Test address",
            Lat = 41.31,
            Lng = 69.24,
            AssignedAt = now.AddHours(-1),
            CreatedAt = now.AddHours(-2)
        });

        var trackingRepository = new InMemoryTrackingRepositoryForWorker();
        // Last location was 5 minutes ago — stale relative to 2-minute threshold
        trackingRepository.Locations.Add(new CourierLocation
        {
            CourierId = courierId,
            OrderId = orderId,
            Lat = 41.311,
            Lng = 69.241,
            CreatedAt = now.AddMinutes(-5)
        });

        var logger = new RecordingLogger<StaleCourierDetectorWorker>();
        var scopeFactory = new FakeServiceScopeFactory(orderRepository, trackingRepository, new FixedClock(now));

        var worker = new StaleCourierDetectorWorker(scopeFactory, logger);
        await worker.RunDetectionForTestAsync(CancellationToken.None);

        Assert.Contains(logger.Entries, e =>
            e.Contains("StaleCourier") && e.Contains(courierId.ToString()));
    }

    [Fact]
    public async Task StaleCourierDetector_DoesNotLog_WhenLocationIsRecent()
    {
        var now = new DateTimeOffset(2026, 4, 15, 18, 0, 0, TimeSpan.Zero);
        var courierId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var orderRepository = new InMemoryOrderRepositoryForWorker();
        orderRepository.Orders.Add(new Order
        {
            Id = orderId, OrderCode = "ORD-FRESH-0001",
            CustomerId = Guid.NewGuid(), BranchId = Guid.NewGuid(),
            CourierId = courierId, Status = OrderStatus.OnTheWay,
            RecipientName = "Test", RecipientPhone = "+998900000002",
            Address = "Test address", Lat = 41.31, Lng = 69.24,
            AssignedAt = now.AddMinutes(-5), CreatedAt = now.AddMinutes(-10)
        });

        var trackingRepository = new InMemoryTrackingRepositoryForWorker();
        // Location 30 seconds ago — fresh
        trackingRepository.Locations.Add(new CourierLocation
        {
            CourierId = courierId,
            OrderId = orderId,
            Lat = 41.312,
            Lng = 69.242,
            CreatedAt = now.AddSeconds(-30)
        });

        var logger = new RecordingLogger<StaleCourierDetectorWorker>();
        var scopeFactory = new FakeServiceScopeFactory(orderRepository, trackingRepository, new FixedClock(now));

        var worker = new StaleCourierDetectorWorker(scopeFactory, logger);
        await worker.RunDetectionForTestAsync(CancellationToken.None);

        Assert.DoesNotContain(logger.Entries, e => e.Contains("StaleCourier"));
    }

    // ──────────────────────────────────────────────
    // Shared fakes
    // ──────────────────────────────────────────────

    private sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly List<RefreshToken> _tokens = new();
        private long _id = 1;

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
            => Task.FromResult(_tokens.FirstOrDefault(t => t.TokenHash == tokenHash));

        public Task AddAsync(RefreshToken token, CancellationToken ct = default)
        {
            if (token.Id == 0) token.Id = _id++;
            _tokens.Add(token);
            return Task.CompletedTask;
        }

        public Task RevokeAsync(long id, DateTimeOffset revokedAt, CancellationToken ct = default)
        {
            var t = _tokens.FirstOrDefault(x => x.Id == id);
            if (t is not null) t.RevokedAt = revokedAt;
            return Task.CompletedTask;
        }

        public Task<int> DeleteExpiredAsync(DateTimeOffset expiredBefore, DateTimeOffset revokedBefore, CancellationToken ct = default)
        {
            var toRemove = _tokens.Where(t =>
                t.ExpiresAt < expiredBefore ||
                (t.RevokedAt.HasValue && t.RevokedAt.Value < revokedBefore)).ToList();
            foreach (var t in toRemove) _tokens.Remove(t);
            return Task.FromResult(toRemove.Count);
        }
    }

    private sealed class InMemoryOrderRepositoryForWorker : IOrderRepository
    {
        public List<Order> Orders { get; } = new();

        public Task AddAsync(Order order, OrderStatusHistory initialHistory, CancellationToken ct = default)
        { Orders.Add(order); return Task.CompletedTask; }

        public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Orders.FirstOrDefault(o => o.Id == id));

        public Task<Order?> GetByIdForCourierAsync(Guid id, Guid courierId, CancellationToken ct = default)
            => Task.FromResult(Orders.FirstOrDefault(o => o.Id == id && o.CourierId == courierId));

        public Task<IReadOnlyList<Order>> SearchAsync(OrderSearchFilter filter, CancellationToken ct = default)
        {
            IEnumerable<Order> q = Orders;
            if (filter.Status.HasValue) q = q.Where(o => o.Status == filter.Status.Value);
            return Task.FromResult<IReadOnlyList<Order>>(q.Take(filter.Limit).ToArray());
        }

        public Task<IReadOnlyList<Order>> GetAssignedToCourierAsync(Guid courierId, bool includeCompleted, int limit, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Order>>(Array.Empty<Order>());

        public Task<bool> UpdateDetailsIfCreatedAsync(Order order, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<bool> AssignCourierIfCreatedAsync(Guid orderId, Guid courierId, Guid changedBy, string? note, DateTimeOffset assignedAt, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<bool> TransitionStatusForCourierAsync(Guid orderId, Guid courierId, OrderStatus expected, OrderStatus next, Guid changedBy, string? note, DateTimeOffset changedAt, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<IReadOnlyList<OrderStatusHistory>> GetStatusHistoryAsync(Guid orderId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OrderStatusHistory>>(Array.Empty<OrderStatusHistory>());
    }

    private sealed class InMemoryTrackingRepositoryForWorker : ITrackingRepository
    {
        public List<CourierLocation> Locations { get; } = new();

        public Task AddAsync(CourierLocation location, CancellationToken ct = default)
        { Locations.Add(location); return Task.CompletedTask; }

        public Task<CourierLocation?> GetLatestByCourierAsync(Guid courierId, CancellationToken ct = default)
            => Task.FromResult(Locations.Where(l => l.CourierId == courierId).OrderByDescending(l => l.CreatedAt).FirstOrDefault());

        public Task<CourierLocation?> GetLatestByOrderAsync(Guid orderId, CancellationToken ct = default)
            => Task.FromResult(Locations.Where(l => l.OrderId == orderId).OrderByDescending(l => l.CreatedAt).FirstOrDefault());
    }

    private sealed class RecordingLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public List<string> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(formatter(state, exception));
        }
    }

    private sealed class FakeServiceScopeFactory : IServiceScopeFactory
    {
        private readonly IOrderRepository _orders;
        private readonly ITrackingRepository _tracking;
        private readonly IClock _clock;

        public FakeServiceScopeFactory(IOrderRepository orders, ITrackingRepository tracking, IClock clock)
        {
            _orders = orders;
            _tracking = tracking;
            _clock = clock;
        }

        public IServiceScope CreateScope() => new FakeServiceScope(_orders, _tracking, _clock);

        private sealed class FakeServiceScope : IServiceScope
        {
            public IServiceProvider ServiceProvider { get; }

            public FakeServiceScope(IOrderRepository orders, ITrackingRepository tracking, IClock clock)
            {
                ServiceProvider = new FakeServiceProvider(orders, tracking, clock);
            }

            public void Dispose() { }
        }

        private sealed class FakeServiceProvider : IServiceProvider
        {
            private readonly IOrderRepository _orders;
            private readonly ITrackingRepository _tracking;
            private readonly IClock _clock;

            public FakeServiceProvider(IOrderRepository orders, ITrackingRepository tracking, IClock clock)
            {
                _orders = orders;
                _tracking = tracking;
                _clock = clock;
            }

            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(IOrderRepository)) return _orders;
                if (serviceType == typeof(ITrackingRepository)) return _tracking;
                if (serviceType == typeof(IClock)) return _clock;
                return null;
            }
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }
}
