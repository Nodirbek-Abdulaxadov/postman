using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }

    public Guid? BranchId { get; set; }

    public UserRole Role { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}