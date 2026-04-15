using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Application.Abstractions.Time;
using PostalDeliverySystem.Application.Common.Exceptions;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;
using PostalDeliverySystem.Shared.Contracts.Branches;

namespace PostalDeliverySystem.Application.Branches;

public sealed class BranchService : IBranchService
{
    private readonly IBranchRepository _branchRepository;
    private readonly IClock _clock;

    public BranchService(IBranchRepository branchRepository, IClock clock)
    {
        _branchRepository = branchRepository;
        _clock = clock;
    }

    public async Task<BranchResponse> CreateAsync(CreateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(request.Code);
        var name = NormalizeRequired(request.Name, "Branch name is required.");

        var existing = await _branchRepository.GetByCodeAsync(code, cancellationToken);
        if (existing is not null)
        {
            throw new ApplicationConflictException($"Branch code '{code}' already exists.");
        }

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            CreatedAt = _clock.UtcNow
        };

        await _branchRepository.AddAsync(branch, cancellationToken);
        return Map(branch);
    }

    public async Task<IReadOnlyList<BranchResponse>> GetAccessibleBranchesAsync(
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorRole == UserRole.SuperAdmin)
        {
            var all = await _branchRepository.GetAllAsync(cancellationToken);
            return all.Select(Map).ToArray();
        }

        if (actorRole == UserRole.Admin)
        {
            if (!actorBranchId.HasValue)
            {
                throw new ApplicationForbiddenException("Admin user is missing branch scope.");
            }

            var ownBranch = await _branchRepository.GetByIdAsync(actorBranchId.Value, cancellationToken);
            if (ownBranch is null)
            {
                throw new ApplicationNotFoundException("Branch not found.");
            }

            return new[] { Map(ownBranch) };
        }

        throw new ApplicationForbiddenException("Only SuperAdmin and Admin can read branches.");
    }

    public async Task<BranchResponse> GetAccessibleByIdAsync(
        Guid branchId,
        UserRole actorRole,
        Guid? actorBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorRole == UserRole.Admin && actorBranchId != branchId)
        {
            throw new ApplicationForbiddenException("Admin can access only their branch.");
        }

        if (actorRole != UserRole.SuperAdmin && actorRole != UserRole.Admin)
        {
            throw new ApplicationForbiddenException("Only SuperAdmin and Admin can read branches.");
        }

        var branch = await _branchRepository.GetByIdAsync(branchId, cancellationToken);
        if (branch is null)
        {
            throw new ApplicationNotFoundException("Branch not found.");
        }

        return Map(branch);
    }

    private static BranchResponse Map(Branch branch)
    {
        return new BranchResponse
        {
            Id = branch.Id,
            Code = branch.Code,
            Name = branch.Name,
            CreatedAt = branch.CreatedAt
        };
    }

    private static string NormalizeCode(string value)
    {
        var normalized = NormalizeRequired(value, "Branch code is required.").ToUpperInvariant();
        if (normalized.Length > 32)
        {
            throw new ApplicationValidationException("Branch code must be at most 32 characters.");
        }

        return normalized;
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