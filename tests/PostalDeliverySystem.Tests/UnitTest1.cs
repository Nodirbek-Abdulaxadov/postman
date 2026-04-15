using System.Security.Cryptography;
using System.Text;
using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Application.Abstractions.Security;
using PostalDeliverySystem.Application.Abstractions.Time;
using PostalDeliverySystem.Application.Auth;
using PostalDeliverySystem.Application.Common.Exceptions;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;
using PostalDeliverySystem.Shared.Contracts.Auth;

namespace PostalDeliverySystem.Tests;

public sealed class UnitTest1
{
    [Fact]
    public async Task BootstrapSuperAdmin_OnlyWorksOnce()
    {
        var userRepository = new InMemoryUserRepository();
        var refreshTokenRepository = new InMemoryRefreshTokenRepository();
        var service = BuildAuthService(
            userRepository,
            refreshTokenRepository,
            new SequenceRefreshTokenGenerator("refresh-token-1", "refresh-token-2"));

        var firstResult = await service.BootstrapSuperAdminAsync(new BootstrapSuperAdminRequest
        {
            FullName = "Root User",
            Phone = "+998900000001",
            Password = "SuperAdmin123!"
        });

        Assert.Equal("refresh-token-1", firstResult.RefreshToken);
        Assert.Equal(UserRole.SuperAdmin, firstResult.User.Role);

        await Assert.ThrowsAsync<ApplicationForbiddenException>(async () =>
            await service.BootstrapSuperAdminAsync(new BootstrapSuperAdminRequest
            {
                FullName = "Second Root",
                Phone = "+998900000002",
                Password = "AnotherPassword123!"
            }));
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndRevokesOldToken()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 4, 15, 10, 30, 0, TimeSpan.Zero));
        var userRepository = new InMemoryUserRepository();
        var refreshTokenRepository = new InMemoryRefreshTokenRepository();
        var generator = new SequenceRefreshTokenGenerator("new-refresh-token");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Role = UserRole.Admin,
            BranchId = Guid.NewGuid(),
            FullName = "Branch Admin",
            Phone = "+998900000010",
            PasswordHash = "hash::password",
            IsActive = true,
            CreatedAt = clock.UtcNow
        };

        await userRepository.AddAsync(user);
        await refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = ComputeSha256("old-refresh-token"),
            ExpiresAt = clock.UtcNow.AddDays(10),
            CreatedAt = clock.UtcNow
        });

        var service = new AuthService(
            userRepository,
            refreshTokenRepository,
            new FakePasswordHasher(),
            generator,
            new FakeJwtTokenService(),
            clock);

        var response = await service.RefreshAsync(new RefreshTokenRequest
        {
            RefreshToken = "old-refresh-token"
        });

        Assert.Equal("new-refresh-token", response.RefreshToken);
        var oldToken = await refreshTokenRepository.GetByTokenHashAsync(ComputeSha256("old-refresh-token"));
        Assert.NotNull(oldToken);
        Assert.NotNull(oldToken!.RevokedAt);
    }

    private static AuthService BuildAuthService(
        InMemoryUserRepository userRepository,
        InMemoryRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenGenerator refreshTokenGenerator)
    {
        return new AuthService(
            userRepository,
            refreshTokenRepository,
            new FakePasswordHasher(),
            refreshTokenGenerator,
            new FakeJwtTokenService(),
            new FixedClock(new DateTimeOffset(2026, 4, 15, 10, 0, 0, TimeSpan.Zero)));
    }

    private static string ComputeSha256(string input)
    {
        var payload = Encoding.UTF8.GetBytes(input);
        var digest = SHA256.HashData(payload);
        return Convert.ToHexString(digest);
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly List<User> _users = new();

        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Id == userId));
        }

        public Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Phone == phone));
        }

        public Task<IReadOnlyList<User>> SearchAsync(UserSearchFilter filter, CancellationToken cancellationToken = default)
        {
            IEnumerable<User> query = _users;

            if (filter.BranchId.HasValue)
            {
                query = query.Where(u => u.BranchId == filter.BranchId.Value);
            }

            if (filter.Role.HasValue)
            {
                query = query.Where(u => u.Role == filter.Role.Value);
            }

            return Task.FromResult<IReadOnlyList<User>>(query.Take(filter.Limit).ToArray());
        }

        public Task<int> CountByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.Count(u => u.Role == role));
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly List<RefreshToken> _tokens = new();
        private long _idSequence = 1;

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tokens.FirstOrDefault(t => t.TokenHash == tokenHash));
        }

        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            if (refreshToken.Id == 0)
            {
                refreshToken.Id = _idSequence;
                _idSequence++;
            }

            _tokens.Add(refreshToken);
            return Task.CompletedTask;
        }

        public Task RevokeAsync(long refreshTokenId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
        {
            var token = _tokens.FirstOrDefault(t => t.Id == refreshTokenId);
            if (token is not null)
            {
                token.RevokedAt = revokedAt;
            }

            return Task.CompletedTask;
        }

        public Task<int> DeleteExpiredAsync(DateTimeOffset expiredBefore, DateTimeOffset revokedBefore, CancellationToken cancellationToken = default)
        {
            var toRemove = _tokens.Where(t =>
                t.ExpiresAt < expiredBefore ||
                (t.RevokedAt.HasValue && t.RevokedAt.Value < revokedBefore)).ToList();

            foreach (var t in toRemove)
            {
                _tokens.Remove(t);
            }

            return Task.FromResult(toRemove.Count);
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return $"hash::{password}";
        }

        public bool Verify(string password, string passwordHash)
        {
            return passwordHash == Hash(password);
        }
    }

    private sealed class SequenceRefreshTokenGenerator : IRefreshTokenGenerator
    {
        private readonly Queue<string> _tokens;

        public SequenceRefreshTokenGenerator(params string[] tokens)
        {
            _tokens = new Queue<string>(tokens);
        }

        public string Generate()
        {
            if (_tokens.Count == 0)
            {
                throw new InvalidOperationException("No token prepared for test.");
            }

            return _tokens.Dequeue();
        }
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public AccessTokenResult GenerateAccessToken(User user, DateTimeOffset now)
        {
            return new AccessTokenResult($"access-token-{user.Id}", now.AddMinutes(15));
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}