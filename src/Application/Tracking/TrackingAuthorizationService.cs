using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Application.Common.Exceptions;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Application.Tracking;

public sealed class TrackingAuthorizationService : ITrackingAuthorizationService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;

    public TrackingAuthorizationService(IOrderRepository orderRepository, IUserRepository userRepository)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
    }

    public async Task<Order> EnsureOrderAccessAsync(
        Guid orderId,
        Guid actorUserId,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Order was not found.");

        switch (actorRole)
        {
            case UserRole.SuperAdmin:
                return order;

            case UserRole.Admin:
                if (!actorBranchId.HasValue || actorBranchId.Value != order.BranchId)
                {
                    throw new ApplicationForbiddenException("Admin can access tracking only in their branch.");
                }

                return order;

            case UserRole.Courier:
                if (order.CourierId != actorUserId)
                {
                    throw new ApplicationForbiddenException("Courier can access tracking only for assigned orders.");
                }

                return order;

            case UserRole.Customer:
                if (order.CustomerId != actorUserId)
                {
                    throw new ApplicationForbiddenException("Customer can access tracking only for their own orders.");
                }

                return order;

            default:
                throw new ApplicationForbiddenException("Role is not allowed to access tracking.");
        }
    }

    public Task EnsureBranchAccessAsync(
        Guid branchId,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorRole == UserRole.SuperAdmin)
        {
            return Task.CompletedTask;
        }

        if (actorRole != UserRole.Admin || !actorBranchId.HasValue || actorBranchId.Value != branchId)
        {
            throw new ApplicationForbiddenException("Only branch-scoped admins can subscribe to this branch stream.");
        }

        return Task.CompletedTask;
    }

    public async Task EnsureCourierAccessAsync(
        Guid courierId,
        Guid actorUserId,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorRole == UserRole.SuperAdmin)
        {
            return;
        }

        if (actorRole == UserRole.Courier)
        {
            if (actorUserId != courierId)
            {
                throw new ApplicationForbiddenException("Courier can subscribe only to their own stream.");
            }

            return;
        }

        if (actorRole != UserRole.Admin || !actorBranchId.HasValue)
        {
            throw new ApplicationForbiddenException("Role is not allowed to subscribe to courier stream.");
        }

        var courier = await _userRepository.GetByIdAsync(courierId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Courier was not found.");

        if (courier.Role != UserRole.Courier || courier.BranchId != actorBranchId)
        {
            throw new ApplicationForbiddenException("Admin can subscribe only to couriers in their branch.");
        }
    }
}
