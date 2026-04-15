using Dapper;
using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Infrastructure.Data;

namespace PostalDeliverySystem.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RefreshTokenRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                user_id AS UserId,
                token_hash AS TokenHash,
                expires_at AS ExpiresAt,
                revoked_at AS RevokedAt,
                created_at AS CreatedAt
            FROM refresh_tokens
            WHERE token_hash = @TokenHash
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<RefreshToken>(
            new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: cancellationToken));
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO refresh_tokens (user_id, token_hash, expires_at, revoked_at, created_at)
            VALUES (@UserId, @TokenHash, @ExpiresAt, @RevokedAt, @CreatedAt);
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, refreshToken, cancellationToken: cancellationToken));
    }

    public async Task RevokeAsync(long refreshTokenId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE refresh_tokens
            SET revoked_at = @RevokedAt
            WHERE id = @RefreshTokenId AND revoked_at IS NULL;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    RefreshTokenId = refreshTokenId,
                    RevokedAt = revokedAt
                },
                cancellationToken: cancellationToken));
    }

    public async Task<int> DeleteExpiredAsync(DateTimeOffset expiredBefore, DateTimeOffset revokedBefore, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM refresh_tokens
            WHERE expires_at < @ExpiredBefore
               OR (revoked_at IS NOT NULL AND revoked_at < @RevokedBefore);
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { ExpiredBefore = expiredBefore, RevokedBefore = revokedBefore },
                cancellationToken: cancellationToken));
    }
}