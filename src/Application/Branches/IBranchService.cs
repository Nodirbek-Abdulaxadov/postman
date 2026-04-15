using PostalDeliverySystem.Domain.Enums;
using PostalDeliverySystem.Shared.Contracts.Branches;

namespace PostalDeliverySystem.Application.Branches;

public interface IBranchService
{
    Task<BranchResponse> CreateAsync(CreateBranchRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BranchResponse>> GetAccessibleBranchesAsync(
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default);

    Task<BranchResponse> GetAccessibleByIdAsync(
        Guid branchId,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default);
}