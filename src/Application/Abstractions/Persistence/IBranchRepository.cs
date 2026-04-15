using PostalDeliverySystem.Domain.Entities;

namespace PostalDeliverySystem.Application.Abstractions.Persistence;

public interface IBranchRepository
{
    Task<Branch?> GetByIdAsync(Guid branchId, CancellationToken cancellationToken = default);

    Task<Branch?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Branch branch, CancellationToken cancellationToken = default);
}