using PostalDeliverySystem.Domain.Entities;

namespace PostalDeliverySystem.Application.Abstractions.Realtime;

public interface ITrackingLocationCache
{
    Task<CourierLocation?> GetLatestByCourierAsync(Guid courierId, CancellationToken cancellationToken = default);

    Task<CourierLocation?> GetLatestByOrderAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task SetLatestAsync(CourierLocation location, CancellationToken cancellationToken = default);
}
