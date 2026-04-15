using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Application.Abstractions.Realtime;
using PostalDeliverySystem.Application.Abstractions.Time;
using PostalDeliverySystem.Application.Common.Exceptions;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;
using PostalDeliverySystem.Shared.Contracts.Tracking;

namespace PostalDeliverySystem.Application.Tracking;

public sealed class TrackingService : ITrackingService
{
    private const double MinMovementMeters = 10;
    private static readonly TimeSpan MinAcceptedInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxDeviceTimeSkew = TimeSpan.FromMinutes(2);

    private readonly IOrderRepository _orderRepository;
    private readonly ITrackingRepository _trackingRepository;
    private readonly ITrackingLocationCache _trackingLocationCache;
    private readonly ITrackingRealtimePublisher _trackingRealtimePublisher;
    private readonly ITrackingAuthorizationService _trackingAuthorizationService;
    private readonly IClock _clock;

    public TrackingService(
        IOrderRepository orderRepository,
        ITrackingRepository trackingRepository,
        ITrackingLocationCache trackingLocationCache,
        ITrackingRealtimePublisher trackingRealtimePublisher,
        ITrackingAuthorizationService trackingAuthorizationService,
        IClock clock)
    {
        _orderRepository = orderRepository;
        _trackingRepository = trackingRepository;
        _trackingLocationCache = trackingLocationCache;
        _trackingRealtimePublisher = trackingRealtimePublisher;
        _trackingAuthorizationService = trackingAuthorizationService;
        _clock = clock;
    }

    public async Task<TrackingLocationIngestResponse> IngestLocationAsync(
        TrackingLocationIngestRequest request,
        Guid actorUserId,
        UserRole actorRole,
        CancellationToken cancellationToken = default)
    {
        if (actorRole != UserRole.Courier)
        {
            throw new ApplicationForbiddenException("Only couriers can submit tracking locations.");
        }

        if (request.CourierId == Guid.Empty || request.OrderId == Guid.Empty)
        {
            throw new ApplicationValidationException("CourierId and OrderId are required.");
        }

        if (request.CourierId != actorUserId)
        {
            throw new ApplicationForbiddenException("Courier can submit tracking only for their own identity.");
        }

        var now = _clock.UtcNow;
        ValidateCoordinates(request.Lat, request.Lng);
        ValidateMetrics(request.AccuracyMeters, request.SpeedMps);
        ValidateDeviceTimeSkew(request.DeviceTime, now);

        var order = await _orderRepository.GetByIdForCourierAsync(request.OrderId, actorUserId, cancellationToken)
            ?? throw new ApplicationForbiddenException("Courier can send tracking only for assigned orders.");

        if (!IsTrackingAllowedStatus(order.Status))
        {
            throw new ApplicationConflictException("Tracking is allowed only for active delivery statuses.");
        }

        var previous = await GetLatestByCourierForThrottleAsync(actorUserId, cancellationToken);
        var shouldAccept = ShouldAcceptLocation(previous, request.Lat, request.Lng, now, out var ignoredReason, out var distanceMeters);

        if (!shouldAccept)
        {
            return new TrackingLocationIngestResponse
            {
                Accepted = false,
                IgnoredReason = ignoredReason,
                RecordedAt = null,
                DistanceMetersFromPrevious = distanceMeters
            };
        }

        var location = new CourierLocation
        {
            CourierId = actorUserId,
            OrderId = request.OrderId,
            Lat = request.Lat,
            Lng = request.Lng,
            AccuracyMeters = request.AccuracyMeters,
            SpeedMps = request.SpeedMps,
            CreatedAt = now
        };

        await _trackingRepository.AddAsync(location, cancellationToken);

        try
        {
            await _trackingLocationCache.SetLatestAsync(location, cancellationToken);
        }
        catch
        {
            // Redis is best-effort for operational latency and must not break durable writes.
        }

        var updateEvent = new TrackingLocationUpdateEvent
        {
            OrderId = request.OrderId,
            CourierId = actorUserId,
            BranchId = order.BranchId,
            Lat = request.Lat,
            Lng = request.Lng,
            RecordedAt = now
        };

        try
        {
            await _trackingRealtimePublisher.PublishLocationUpdateAsync(updateEvent, cancellationToken);
        }
        catch
        {
            // Realtime fan-out is best-effort and should not fail the accepted tracking write.
        }

        return new TrackingLocationIngestResponse
        {
            Accepted = true,
            IgnoredReason = null,
            RecordedAt = now,
            DistanceMetersFromPrevious = distanceMeters
        };
    }

    public async Task<TrackingLocationResponse?> GetLatestOrderLocationAsync(
        Guid orderId,
        Guid actorUserId,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
        {
            throw new ApplicationValidationException("OrderId is required.");
        }

        var order = await _trackingAuthorizationService.EnsureOrderAccessAsync(
            orderId,
            actorUserId,
            actorRole,
            actorBranchId,
            cancellationToken);

        CourierLocation? latest = null;
        try
        {
            latest = await _trackingLocationCache.GetLatestByOrderAsync(orderId, cancellationToken);
        }
        catch
        {
            latest = null;
        }

        latest ??= await _trackingRepository.GetLatestByOrderAsync(orderId, cancellationToken);
        if (latest is null || !latest.OrderId.HasValue)
        {
            return null;
        }

        return new TrackingLocationResponse
        {
            CourierId = latest.CourierId,
            OrderId = latest.OrderId.Value,
            BranchId = order.BranchId,
            Lat = latest.Lat,
            Lng = latest.Lng,
            AccuracyMeters = latest.AccuracyMeters,
            SpeedMps = latest.SpeedMps,
            RecordedAt = latest.CreatedAt
        };
    }

    private async Task<CourierLocation?> GetLatestByCourierForThrottleAsync(
        Guid courierId,
        CancellationToken cancellationToken)
    {
        CourierLocation? latest = null;
        try
        {
            latest = await _trackingLocationCache.GetLatestByCourierAsync(courierId, cancellationToken);
        }
        catch
        {
            latest = null;
        }

        return latest ?? await _trackingRepository.GetLatestByCourierAsync(courierId, cancellationToken);
    }

    private static bool ShouldAcceptLocation(
        CourierLocation? previous,
        double lat,
        double lng,
        DateTimeOffset now,
        out string? ignoredReason,
        out double? distanceMeters)
    {
        ignoredReason = null;
        distanceMeters = null;

        if (previous is null)
        {
            return true;
        }

        var elapsed = now - previous.CreatedAt;
        if (elapsed < MinAcceptedInterval)
        {
            ignoredReason = "min_interval_not_reached";
            return false;
        }

        var distance = CalculateDistanceMeters(previous.Lat, previous.Lng, lat, lng);
        distanceMeters = distance;

        if (distance < MinMovementMeters && elapsed < HeartbeatInterval)
        {
            ignoredReason = "movement_below_threshold";
            return false;
        }

        return true;
    }

    private static bool IsTrackingAllowedStatus(OrderStatus status)
    {
        return status is OrderStatus.Assigned or OrderStatus.Accepted or OrderStatus.PickedUp or OrderStatus.OnTheWay;
    }

    private static void ValidateCoordinates(double lat, double lng)
    {
        if (double.IsNaN(lat) || double.IsInfinity(lat) || lat < -90 || lat > 90)
        {
            throw new ApplicationValidationException("Latitude must be between -90 and 90.");
        }

        if (double.IsNaN(lng) || double.IsInfinity(lng) || lng < -180 || lng > 180)
        {
            throw new ApplicationValidationException("Longitude must be between -180 and 180.");
        }
    }

    private static void ValidateMetrics(float? accuracyMeters, float? speedMps)
    {
        if (accuracyMeters.HasValue && (float.IsNaN(accuracyMeters.Value) || accuracyMeters.Value < 0))
        {
            throw new ApplicationValidationException("Accuracy meters must be zero or positive.");
        }

        if (speedMps.HasValue && (float.IsNaN(speedMps.Value) || speedMps.Value < 0))
        {
            throw new ApplicationValidationException("Speed mps must be zero or positive.");
        }
    }

    private static void ValidateDeviceTimeSkew(DateTimeOffset deviceTime, DateTimeOffset now)
    {
        if (deviceTime == default)
        {
            throw new ApplicationValidationException("Device time is required.");
        }

        var skew = now - deviceTime;
        if (skew.Duration() > MaxDeviceTimeSkew)
        {
            throw new ApplicationValidationException("Device time skew is too large.");
        }
    }

    private static double CalculateDistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusMeters = 6371000;

        var lat1Rad = DegreesToRadians(lat1);
        var lat2Rad = DegreesToRadians(lat2);
        var deltaLat = DegreesToRadians(lat2 - lat1);
        var deltaLng = DegreesToRadians(lng2 - lng1);

        var sinLat = Math.Sin(deltaLat / 2);
        var sinLng = Math.Sin(deltaLng / 2);

        var a = sinLat * sinLat + Math.Cos(lat1Rad) * Math.Cos(lat2Rad) * sinLng * sinLng;
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusMeters * c;
    }

    private static double DegreesToRadians(double value)
    {
        return value * Math.PI / 180d;
    }
}
