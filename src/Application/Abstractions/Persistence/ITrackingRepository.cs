using PostalDeliverySystem.Domain.Entities;

namespace PostalDeliverySystem.Application.Abstractions.Persistence;

public interface ITrackingRepository
{
    Task AddAsync(CourierLocation location, CancellationToken cancellationToken = default);

    Task<CourierLocation?> GetLatestByCourierAsync(Guid courierId, CancellationToken cancellationToken = default);

    Task<CourierLocation?> GetLatestByOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
}
