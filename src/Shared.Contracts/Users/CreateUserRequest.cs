using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Shared.Contracts.Users;

public sealed class CreateUserRequest
{
    public Guid? BranchId { get; set; }

    public UserRole Role { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}