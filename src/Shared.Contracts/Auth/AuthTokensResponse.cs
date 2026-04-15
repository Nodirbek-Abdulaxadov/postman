namespace PostalDeliverySystem.Shared.Contracts.Auth;

public sealed class AuthTokensResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTimeOffset AccessTokenExpiresAt { get; set; }

    public string RefreshToken { get; set; } = string.Empty;

    public DateTimeOffset RefreshTokenExpiresAt { get; set; }

    public CurrentUserResponse User { get; set; } = new();
}