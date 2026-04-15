namespace PostalDeliverySystem.Shared.Contracts.Auth;

public sealed class LoginRequest
{
    public string Phone { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}