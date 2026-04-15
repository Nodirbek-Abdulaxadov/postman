using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> SearchAsync(UserSearchFilter filter, CancellationToken cancellationToken = default);

    Task<int> CountByRoleAsync(UserRole role, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}