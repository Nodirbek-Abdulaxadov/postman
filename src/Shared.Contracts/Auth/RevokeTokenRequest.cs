namespace PostalDeliverySystem.Shared.Contracts.Auth;

public sealed class RevokeTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}