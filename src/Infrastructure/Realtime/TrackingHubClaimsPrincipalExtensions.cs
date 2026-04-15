using System.Security.Claims;
using PostalDeliverySystem.Application.Common.Exceptions;
using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Infrastructure.Realtime;

internal static class TrackingHubClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(userId, out var parsed))
        {
            throw new ApplicationUnauthorizedException("Authenticated user id is missing.");
        }

        return parsed;
    }

    public static UserRole GetRequiredRole(this ClaimsPrincipal principal)
    {
        var roleValue = principal.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrWhiteSpace(roleValue) || !Enum.TryParse<UserRole>(roleValue, true, out var role))
        {
            throw new ApplicationUnauthorizedException("Authenticated user role is missing.");
        }

        return role;
    }

    public static Guid? GetOptionalBranchId(this ClaimsPrincipal principal)
    {
        var branchId = principal.FindFirstValue("branch_id");
        if (string.IsNullOrWhiteSpace(branchId))
        {
            return null;
        }

        if (!Guid.TryParse(branchId, out var parsed))
        {
            throw new ApplicationUnauthorizedException("Authenticated branch scope is invalid.");
        }

        return parsed;
    }
}
