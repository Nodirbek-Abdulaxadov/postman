using PostalDeliverySystem.Domain.Entities;

namespace PostalDeliverySystem.Application.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task RevokeAsync(long refreshTokenId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes refresh tokens that have expired or were revoked before <paramref name="revokedBefore"/>.
    /// Returns the number of rows deleted.
    /// </summary>
    Task<int> DeleteExpiredAsync(DateTimeOffset expiredBefore, DateTimeOffset revokedBefore, CancellationToken cancellationToken = default);
}
