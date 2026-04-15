using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Application.Abstractions.Persistence;

public sealed class UserSearchFilter
{
    public Guid? BranchId { get; set; }

    public UserRole? Role { get; set; }

    public int Limit { get; set; } = 100;
}