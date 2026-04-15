using Dapper;
using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;
using PostalDeliverySystem.Infrastructure.Data;

namespace PostalDeliverySystem.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                branch_id AS BranchId,
                role AS Role,
                full_name AS FullName,
                phone AS Phone,
                password_hash AS PasswordHash,
                is_active AS IsActive,
                created_at AS CreatedAt
            FROM users
            WHERE id = @UserId
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                branch_id AS BranchId,
                role AS Role,
                full_name AS FullName,
                phone AS Phone,
                password_hash AS PasswordHash,
                is_active AS IsActive,
                created_at AS CreatedAt
            FROM users
            WHERE phone = @Phone
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { Phone = phone }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<User>> SearchAsync(UserSearchFilter filter, CancellationToken cancellationToken = default)
    {
        const string baseSql = """
            SELECT
                id AS Id,
                branch_id AS BranchId,
                role AS Role,
                full_name AS FullName,
                phone AS Phone,
                password_hash AS PasswordHash,
                is_active AS IsActive,
                created_at AS CreatedAt
            FROM users
            """;

        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (filter.BranchId.HasValue)
        {
            whereClauses.Add("branch_id = @BranchId");
            parameters.Add("BranchId", filter.BranchId.Value);
        }

        if (filter.Role.HasValue)
        {
            whereClauses.Add("role = @Role");
            parameters.Add("Role", filter.Role.Value);
        }

        var sql = baseSql;
        if (whereClauses.Count > 0)
        {
            sql += $" WHERE {string.Join(" AND ", whereClauses)}";
        }

        sql += " ORDER BY created_at DESC LIMIT @Limit;";
        parameters.Add("Limit", filter.Limit);

        using var connection = _connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<User>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return items.AsList();
    }

    public async Task<int> CountByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM users
            WHERE role = @Role;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Role = role }, cancellationToken: cancellationToken));
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO users (id, branch_id, role, full_name, phone, password_hash, is_active, created_at)
            VALUES (@Id, @BranchId, @Role, @FullName, @Phone, @PasswordHash, @IsActive, @CreatedAt);
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, user, cancellationToken: cancellationToken));
    }
}