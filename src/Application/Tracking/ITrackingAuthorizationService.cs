using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Application.Tracking;

public interface ITrackingAuthorizationService
{
    Task<Order> EnsureOrderAccessAsync(
        Guid orderId,
        Guid actorUserId,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default);

    Task EnsureBranchAccessAsync(
        Guid branchId,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default);

    Task EnsureCourierAccessAsync(
        Guid courierId,
        Guid actorUserId,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default);
}
