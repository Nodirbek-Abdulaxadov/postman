using System.Text.Json;
using PostalDeliverySystem.Shared.Contracts.Auth;

namespace PostalDeliverySystem.CourierMobile.Services;

public sealed class CourierSessionService
{
    private const string AccessTokenKey = "courier.session.access-token";
    private const string RefreshTokenKey = "courier.session.refresh-token";
    private const string UserPayloadKey = "courier.session.user";

    private readonly SemaphoreSlim _guard = new(1, 1);
    private bool _initialized;

    public string AccessToken { get; private set; } = string.Empty;

    public string RefreshToken { get; private set; } = string.Empty;

    public CurrentUserResponse? CurrentUser { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

    public async Task EnsureInitializedAsync(CourierApiClient apiClient, CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await _guard.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            AccessToken = await SecureStorage.Default.GetAsync(AccessTokenKey) ?? string.Empty;
            RefreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey) ?? string.Empty;

            var userPayload = await SecureStorage.Default.GetAsync(UserPayloadKey);
            if (!string.IsNullOrWhiteSpace(userPayload))
            {
                CurrentUser = JsonSerializer.Deserialize<CurrentUserResponse>(userPayload);
            }

            if (!string.IsNullOrWhiteSpace(RefreshToken))
            {
                try
                {
                    var refreshed = await apiClient.RefreshWithRawTokenAsync(RefreshToken, cancellationToken);
                    await SetAsync(refreshed);
                }
                catch
                {
                    await ClearAsync();
                }
            }

            _initialized = true;
        }
        finally
        {
            _guard.Release();
        }
    }

    public async Task SetAsync(AuthTokensResponse tokens)
    {
        AccessToken = tokens.AccessToken;
        RefreshToken = tokens.RefreshToken;
        CurrentUser = tokens.User;

        await SecureStorage.Default.SetAsync(AccessTokenKey, AccessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, RefreshToken);
        await SecureStorage.Default.SetAsync(UserPayloadKey, JsonSerializer.Serialize(CurrentUser));
    }

    public async Task ClearAsync()
    {
        AccessToken = string.Empty;
        RefreshToken = string.Empty;
        CurrentUser = null;

        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(UserPayloadKey);

        await Task.CompletedTask;
    }
}