using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Shared.Contracts.Users;

public sealed class UserFilterRequest
{
    public Guid? BranchId { get; set; }

    public UserRole? Role { get; set; }

    public int Limit { get; set; } = 100;
}