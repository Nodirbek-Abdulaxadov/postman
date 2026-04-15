using PostalDeliverySystem.Shared.Contracts.Auth;

namespace PostalDeliverySystem.CustomerWeb.Services;

public sealed class CustomerSession
{
    public string AccessToken { get; private set; } = string.Empty;

    public string RefreshToken { get; private set; } = string.Empty;

    public CurrentUserResponse? CurrentUser { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

    public void Set(AuthTokensResponse response)
    {
        AccessToken = response.AccessToken;
        RefreshToken = response.RefreshToken;
        CurrentUser = response.User;
    }

    public void Clear()
    {
        AccessToken = string.Empty;
        RefreshToken = string.Empty;
        CurrentUser = null;
    }
}
