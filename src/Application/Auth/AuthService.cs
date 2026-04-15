using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Application.Abstractions.Security;
using PostalDeliverySystem.Application.Abstractions.Time;
using PostalDeliverySystem.Application.Common.Exceptions;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;
using PostalDeliverySystem.Shared.Contracts.Auth;

namespace PostalDeliverySystem.Application.Auth;

public sealed class AuthService : IAuthService
{
    private const int RefreshTokenLifetimeDays = 30;

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IClock _clock;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IRefreshTokenGenerator refreshTokenGenerator,
        IJwtTokenService jwtTokenService,
        IClock clock)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _refreshTokenGenerator = refreshTokenGenerator;
        _jwtTokenService = jwtTokenService;
        _clock = clock;
    }

    public async Task<AuthTokensResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var phone = NormalizeRequired(request.Phone, "Phone is required.");
        var password = NormalizeRequired(request.Password, "Password is required.");

        var user = await _userRepository.GetByPhoneAsync(phone, cancellationToken);
        if (user is null || !user.IsActive || !_passwordHasher.Verify(password, user.PasswordHash))
        {
            throw new ApplicationUnauthorizedException("Invalid credentials.");
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthTokensResponse> BootstrapSuperAdminAsync(
        BootstrapSuperAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        var existingSuperAdminCount = await _userRepository.CountByRoleAsync(UserRole.SuperAdmin, cancellationToken);
        if (existingSuperAdminCount > 0)
        {
            throw new ApplicationForbiddenException("SuperAdmin bootstrap is already completed.");
        }

        var fullName = NormalizeRequired(request.FullName, "Full name is required.");
        var phone = NormalizeRequired(request.Phone, "Phone is required.");
        var password = NormalizeRequired(request.Password, "Password is required.");

        var existingByPhone = await _userRepository.GetByPhoneAsync(phone, cancellationToken);
        if (existingByPhone is not null)
        {
            throw new ApplicationConflictException("Phone number already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            BranchId = null,
            Role = UserRole.SuperAdmin,
            FullName = fullName,
            Phone = phone,
            PasswordHash = _passwordHasher.Hash(password),
            IsActive = true,
            CreatedAt = _clock.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthTokensResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var rawRefreshToken = NormalizeRequired(request.RefreshToken, "Refresh token is required.");
        var refreshTokenHash = TokenHashing.ComputeSha256(rawRefreshToken);
        var storedRefreshToken = await _refreshTokenRepository.GetByTokenHashAsync(refreshTokenHash, cancellationToken);

        var now = _clock.UtcNow;
        if (storedRefreshToken is null || storedRefreshToken.RevokedAt.HasValue || storedRefreshToken.ExpiresAt <= now)
        {
            throw new ApplicationUnauthorizedException("Refresh token is invalid or expired.");
        }

        var user = await _userRepository.GetByIdAsync(storedRefreshToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new ApplicationUnauthorizedException("User is not active.");
        }

        await _refreshTokenRepository.RevokeAsync(storedRefreshToken.Id, now, cancellationToken);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task RevokeRefreshTokenAsync(RevokeTokenRequest request, CancellationToken cancellationToken = default)
    {
        var rawRefreshToken = NormalizeRequired(request.RefreshToken, "Refresh token is required.");
        var refreshTokenHash = TokenHashing.ComputeSha256(rawRefreshToken);
        var storedRefreshToken = await _refreshTokenRepository.GetByTokenHashAsync(refreshTokenHash, cancellationToken);

        if (storedRefreshToken is null || storedRefreshToken.RevokedAt.HasValue)
        {
            return;
        }

        await _refreshTokenRepository.RevokeAsync(storedRefreshToken.Id, _clock.UtcNow, cancellationToken);
    }

    private async Task<AuthTokensResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var accessToken = _jwtTokenService.GenerateAccessToken(user, now);
        var rawRefreshToken = _refreshTokenGenerator.Generate();
        var refreshTokenExpiresAt = now.AddDays(RefreshTokenLifetimeDays);

        await _refreshTokenRepository.AddAsync(
            new RefreshToken
            {
                UserId = user.Id,
                TokenHash = TokenHashing.ComputeSha256(rawRefreshToken),
                ExpiresAt = refreshTokenExpiresAt,
                CreatedAt = now
            },
            cancellationToken);

        return new AuthTokensResponse
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            User = new CurrentUserResponse
            {
                Id = user.Id,
                BranchId = user.BranchId,
                FullName = user.FullName,
                Phone = user.Phone,
                Role = user.Role
            }
        };
    }

    private static string NormalizeRequired(string value, string errorMessage)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ApplicationValidationException(errorMessage);
        }

        return normalized;
    }
}