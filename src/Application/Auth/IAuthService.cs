using PostalDeliverySystem.Shared.Contracts.Auth;

namespace PostalDeliverySystem.Application.Auth;

public interface IAuthService
{
    Task<AuthTokensResponse> BootstrapSuperAdminAsync(
        BootstrapSuperAdminRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthTokensResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthTokensResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task RevokeRefreshTokenAsync(RevokeTokenRequest request, CancellationToken cancellationToken = default);
}