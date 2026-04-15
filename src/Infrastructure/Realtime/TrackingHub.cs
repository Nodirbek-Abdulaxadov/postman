using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PostalDeliverySystem.Application.Common.Exceptions;
using PostalDeliverySystem.Application.Tracking;
using PostalDeliverySystem.Shared.Contracts.Tracking;

namespace PostalDeliverySystem.Infrastructure.Realtime;

[Authorize]
public sealed class TrackingHub : Hub
{
    private readonly ITrackingAuthorizationService _trackingAuthorizationService;

    public TrackingHub(ITrackingAuthorizationService trackingAuthorizationService)
    {
        _trackingAuthorizationService = trackingAuthorizationService;
    }

    public Task JoinOrder(Guid orderId)
    {
        return ExecuteWithHubErrorsAsync(async () =>
        {
            var user = Context.User;
            if (user is null)
            {
                throw new ApplicationUnauthorizedException("Authenticated user is missing.");
            }

            await _trackingAuthorizationService.EnsureOrderAccessAsync(
                orderId,
                user.GetRequiredUserId(),
                user.GetRequiredRole(),
                user.GetOptionalBranchId(),
                Context.ConnectionAborted);

            await Groups.AddToGroupAsync(Context.ConnectionId, TrackingGroupNames.Order(orderId));
        });
    }

    public Task LeaveOrder(Guid orderId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, TrackingGroupNames.Order(orderId));
    }

    public Task JoinBranch(Guid branchId)
    {
        return ExecuteWithHubErrorsAsync(async () =>
        {
            var user = Context.User;
            if (user is null)
            {
                throw new ApplicationUnauthorizedException("Authenticated user is missing.");
            }

            await _trackingAuthorizationService.EnsureBranchAccessAsync(
                branchId,
                user.GetRequiredRole(),
                user.GetOptionalBranchId(),
                Context.ConnectionAborted);

            await Groups.AddToGroupAsync(Context.ConnectionId, TrackingGroupNames.Branch(branchId));
        });
    }

    public Task LeaveBranch(Guid branchId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, TrackingGroupNames.Branch(branchId));
    }

    public Task JoinCourier(Guid courierId)
    {
        return ExecuteWithHubErrorsAsync(async () =>
        {
            var user = Context.User;
            if (user is null)
            {
                throw new ApplicationUnauthorizedException("Authenticated user is missing.");
            }

            await _trackingAuthorizationService.EnsureCourierAccessAsync(
                courierId,
                user.GetRequiredUserId(),
                user.GetRequiredRole(),
                user.GetOptionalBranchId(),
                Context.ConnectionAborted);

            await Groups.AddToGroupAsync(Context.ConnectionId, TrackingGroupNames.Courier(courierId));
        });
    }

    public Task LeaveCourier(Guid courierId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, TrackingGroupNames.Courier(courierId));
    }

    private static async Task ExecuteWithHubErrorsAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (ApplicationValidationException ex)
        {
            throw new HubException(ex.Message);
        }
        catch (ApplicationUnauthorizedException ex)
        {
            throw new HubException(ex.Message);
        }
        catch (ApplicationForbiddenException ex)
        {
            throw new HubException(ex.Message);
        }
        catch (ApplicationNotFoundException ex)
        {
            throw new HubException(ex.Message);
        }
        catch (ApplicationConflictException ex)
        {
            throw new HubException(ex.Message);
        }
    }
}
