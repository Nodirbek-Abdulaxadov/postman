using System.Security.Claims;
using PostalDeliverySystem.Application.Common.Exceptions;
using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Api.Security;

public static class ClaimsPrincipalExtensions
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

    public static string GetRequiredFullName(this ClaimsPrincipal principal)
    {
        var fullName = principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue("name");

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ApplicationUnauthorizedException("Authenticated full name is missing.");
        }

        return fullName;
    }

    public static string GetRequiredPhone(this ClaimsPrincipal principal)
    {
        var phone = principal.FindFirstValue(ClaimTypes.MobilePhone)
            ?? principal.FindFirstValue("phone_number");

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new ApplicationUnauthorizedException("Authenticated phone is missing.");
        }

        return phone;
    }
}