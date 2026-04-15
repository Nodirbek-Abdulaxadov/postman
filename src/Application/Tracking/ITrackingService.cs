using PostalDeliverySystem.Domain.Enums;
using PostalDeliverySystem.Shared.Contracts.Tracking;

namespace PostalDeliverySystem.Application.Tracking;

public interface ITrackingService
{
    Task<TrackingLocationIngestResponse> IngestLocationAsync(
        TrackingLocationIngestRequest request,
        Guid actorUserId,
        UserRole actorRole,
        CancellationToken cancellationToken = default);

    Task<TrackingLocationResponse?> GetLatestOrderLocationAsync(
        Guid orderId,
        Guid actorUserId,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default);
}
