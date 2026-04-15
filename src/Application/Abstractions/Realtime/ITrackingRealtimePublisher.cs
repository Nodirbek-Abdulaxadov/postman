using PostalDeliverySystem.Shared.Contracts.Tracking;

namespace PostalDeliverySystem.Application.Abstractions.Realtime;

public interface ITrackingRealtimePublisher
{
    Task PublishLocationUpdateAsync(TrackingLocationUpdateEvent payload, CancellationToken cancellationToken = default);

    Task PublishStatusUpdateAsync(TrackingStatusUpdateEvent payload, CancellationToken cancellationToken = default);
}
