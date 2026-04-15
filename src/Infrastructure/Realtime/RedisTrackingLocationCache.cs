using System.Text.Json;
using PostalDeliverySystem.Application.Abstractions.Realtime;
using PostalDeliverySystem.Domain.Entities;
using StackExchange.Redis;

namespace PostalDeliverySystem.Infrastructure.Realtime;

public sealed class RedisTrackingLocationCache : ITrackingLocationCache
{
    private static readonly TimeSpan EntryTtl = TimeSpan.FromMinutes(30);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisTrackingLocationCache(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<CourierLocation?> GetLatestByCourierAsync(Guid courierId, CancellationToken cancellationToken = default)
    {
        var database = _connectionMultiplexer.GetDatabase();

        var payload = await database.StringGetAsync(GetCourierLatestKey(courierId));
        if (!payload.HasValue)
        {
            payload = await database.StringGetAsync(GetTrackingCourierKey(courierId));
        }

        return Deserialize(payload);
    }

    public async Task<CourierLocation?> GetLatestByOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var database = _connectionMultiplexer.GetDatabase();

        var payload = await database.StringGetAsync(GetOrderLatestKey(orderId));
        if (!payload.HasValue)
        {
            payload = await database.StringGetAsync(GetTrackingOrderKey(orderId));
        }

        return Deserialize(payload);
    }

    public async Task SetLatestAsync(CourierLocation location, CancellationToken cancellationToken = default)
    {
        var database = _connectionMultiplexer.GetDatabase();

        var payload = JsonSerializer.Serialize(new CachedLocation
        {
            CourierId = location.CourierId,
            OrderId = location.OrderId,
            Lat = location.Lat,
            Lng = location.Lng,
            AccuracyMeters = location.AccuracyMeters,
            SpeedMps = location.SpeedMps,
            RecordedAt = location.CreatedAt
        }, JsonOptions);

        var tasks = new List<Task>
        {
            database.StringSetAsync(GetCourierLatestKey(location.CourierId), payload, EntryTtl),
            database.StringSetAsync(GetTrackingCourierKey(location.CourierId), payload, EntryTtl)
        };

        if (location.OrderId.HasValue)
        {
            tasks.Add(database.StringSetAsync(GetOrderLatestKey(location.OrderId.Value), payload, EntryTtl));
            tasks.Add(database.StringSetAsync(GetTrackingOrderKey(location.OrderId.Value), payload, EntryTtl));
        }

        await Task.WhenAll(tasks);
    }

    private static CourierLocation? Deserialize(RedisValue payload)
    {
        if (!payload.HasValue)
        {
            return null;
        }

        var item = JsonSerializer.Deserialize<CachedLocation>(payload!, JsonOptions);
        if (item is null)
        {
            return null;
        }

        return new CourierLocation
        {
            CourierId = item.CourierId,
            OrderId = item.OrderId,
            Lat = item.Lat,
            Lng = item.Lng,
            AccuracyMeters = item.AccuracyMeters,
            SpeedMps = item.SpeedMps,
            CreatedAt = item.RecordedAt
        };
    }

    private static string GetCourierLatestKey(Guid courierId) => $"courier:{courierId}:latest-location";

    private static string GetOrderLatestKey(Guid orderId) => $"order:{orderId}:latest-location";

    private static string GetTrackingCourierKey(Guid courierId) => $"tracking:courier:{courierId}";

    private static string GetTrackingOrderKey(Guid orderId) => $"tracking:order:{orderId}";

    private sealed class CachedLocation
    {
        public Guid CourierId { get; set; }

        public Guid? OrderId { get; set; }

        public double Lat { get; set; }

        public double Lng { get; set; }

        public float? AccuracyMeters { get; set; }

        public float? SpeedMps { get; set; }

        public DateTimeOffset RecordedAt { get; set; }
    }
}
