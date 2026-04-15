using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Application.Abstractions.Security;
using PostalDeliverySystem.Application.Abstractions.Time;
using PostalDeliverySystem.Application.Common.Exceptions;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;
using PostalDeliverySystem.Shared.Contracts.Users;

namespace PostalDeliverySystem.Application.Users;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClock _clock;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, IClock clock)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task<UserResponse> CreateAsync(
        CreateUserRequest request,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorRole != UserRole.SuperAdmin && actorRole != UserRole.Admin)
        {
            throw new ApplicationForbiddenException("Only SuperAdmin and Admin can create users.");
        }

        var fullName = NormalizeRequired(request.FullName, "Full name is required.");
        var phone = NormalizeRequired(request.Phone, "Phone is required.");
        var password = NormalizeRequired(request.Password, "Password is required.");
        var targetRole = request.Role;
        var targetBranchId = request.BranchId;

        if (targetRole == UserRole.SuperAdmin && targetBranchId.HasValue)
        {
            throw new ApplicationValidationException("SuperAdmin must not be assigned to a branch.");
        }

        if (targetRole != UserRole.SuperAdmin && !targetBranchId.HasValue)
        {
            throw new ApplicationValidationException("Branch is required for Admin, Courier, and Customer users.");
        }

        if (actorRole == UserRole.Admin)
        {
            if (!actorBranchId.HasValue)
            {
                throw new ApplicationForbiddenException("Admin user is missing branch scope.");
            }

            if (targetRole == UserRole.SuperAdmin || targetRole == UserRole.Admin)
            {
                throw new ApplicationForbiddenException("Admin cannot create SuperAdmin or Admin users.");
            }

            if (!targetBranchId.HasValue || targetBranchId.Value != actorBranchId.Value)
            {
                throw new ApplicationForbiddenException("Admin can create users only in their own branch.");
            }
        }

        var existing = await _userRepository.GetByPhoneAsync(phone, cancellationToken);
        if (existing is not null)
        {
            throw new ApplicationConflictException("Phone number already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            BranchId = targetBranchId,
            Role = targetRole,
            FullName = fullName,
            Phone = phone,
            PasswordHash = _passwordHasher.Hash(password),
            IsActive = true,
            CreatedAt = _clock.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);
        return Map(user);
    }

    public async Task<IReadOnlyList<UserResponse>> SearchAsync(
        UserFilterRequest request,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorRole != UserRole.SuperAdmin && actorRole != UserRole.Admin)
        {
            throw new ApplicationForbiddenException("Only SuperAdmin and Admin can query users.");
        }

        if (actorRole == UserRole.Admin)
        {
            if (!actorBranchId.HasValue)
            {
                throw new ApplicationForbiddenException("Admin user is missing branch scope.");
            }

            if (request.BranchId.HasValue && request.BranchId.Value != actorBranchId.Value)
            {
                throw new ApplicationForbiddenException("Admin can query users only in their branch.");
            }

            if (request.Role == UserRole.SuperAdmin)
            {
                throw new ApplicationForbiddenException("Admin cannot query SuperAdmin users.");
            }
        }

        var branchId = actorRole == UserRole.Admin ? actorBranchId : request.BranchId;
        var limit = Math.Clamp(request.Limit <= 0 ? 100 : request.Limit, 1, 200);

        var items = await _userRepository.SearchAsync(
            new UserSearchFilter
            {
                BranchId = branchId,
                Role = request.Role,
                Limit = limit
            },
            cancellationToken);

        return items.Select(Map).ToArray();
    }

    public async Task<UserResponse> GetAccessibleByIdAsync(
        Guid userId,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorRole != UserRole.SuperAdmin && actorRole != UserRole.Admin)
        {
            throw new ApplicationForbiddenException("Only SuperAdmin and Admin can access users.");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new ApplicationNotFoundException("User not found.");
        }

        if (actorRole == UserRole.Admin)
        {
            if (!actorBranchId.HasValue || user.BranchId != actorBranchId)
            {
                throw new ApplicationForbiddenException("Admin can access users only in their branch.");
            }
        }

        return Map(user);
    }

    private static UserResponse Map(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            BranchId = user.BranchId,
            Role = user.Role,
            FullName = user.FullName,
            Phone = user.Phone,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
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