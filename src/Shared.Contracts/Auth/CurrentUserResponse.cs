using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Shared.Contracts.Auth;

public sealed class CurrentUserResponse
{
    public Guid Id { get; set; }

    public Guid? BranchId { get; set; }

    public UserRole Role { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;
}