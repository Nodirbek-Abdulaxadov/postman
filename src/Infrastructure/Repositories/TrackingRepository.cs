using Dapper;
using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Infrastructure.Data;

namespace PostalDeliverySystem.Infrastructure.Repositories;

public sealed class TrackingRepository : ITrackingRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TrackingRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(CourierLocation location, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO courier_locations (
                courier_id,
                order_id,
                lat,
                lng,
                accuracy_meters,
                speed_mps,
                created_at)
            VALUES (
                @CourierId,
                @OrderId,
                @Lat,
                @Lng,
                @AccuracyMeters,
                @SpeedMps,
                @CreatedAt);
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, location, cancellationToken: cancellationToken));
    }

    public async Task<CourierLocation?> GetLatestByCourierAsync(Guid courierId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                courier_id AS CourierId,
                order_id AS OrderId,
                lat AS Lat,
                lng AS Lng,
                accuracy_meters AS AccuracyMeters,
                speed_mps AS SpeedMps,
                created_at AS CreatedAt
            FROM courier_locations
            WHERE courier_id = @CourierId
            ORDER BY created_at DESC
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<CourierLocation>(
            new CommandDefinition(sql, new { CourierId = courierId }, cancellationToken: cancellationToken));
    }

    public async Task<CourierLocation?> GetLatestByOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                courier_id AS CourierId,
                order_id AS OrderId,
                lat AS Lat,
                lng AS Lng,
                accuracy_meters AS AccuracyMeters,
                speed_mps AS SpeedMps,
                created_at AS CreatedAt
            FROM courier_locations
            WHERE order_id = @OrderId
            ORDER BY created_at DESC
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<CourierLocation>(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken));
    }
}
