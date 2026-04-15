using PostalDeliverySystem.Domain.Enums;
using PostalDeliverySystem.Shared.Contracts.Users;

namespace PostalDeliverySystem.Application.Users;

public interface IUserService
{
    Task<UserResponse> CreateAsync(
        CreateUserRequest request,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserResponse>> SearchAsync(
        UserFilterRequest request,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default);

    Task<UserResponse> GetAccessibleByIdAsync(
        Guid userId,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default);
}