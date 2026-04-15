namespace PostalDeliverySystem.Shared.Contracts.Auth;

public sealed class BootstrapSuperAdminRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
