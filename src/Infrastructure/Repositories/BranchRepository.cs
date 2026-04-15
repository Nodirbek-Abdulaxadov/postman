using Dapper;
using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Infrastructure.Data;

namespace PostalDeliverySystem.Infrastructure.Repositories;

public sealed class BranchRepository : IBranchRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public BranchRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Branch?> GetByIdAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                code AS Code,
                name AS Name,
                created_at AS CreatedAt
            FROM branches
            WHERE id = @BranchId
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Branch>(
            new CommandDefinition(sql, new { BranchId = branchId }, cancellationToken: cancellationToken));
    }

    public async Task<Branch?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                code AS Code,
                name AS Name,
                created_at AS CreatedAt
            FROM branches
            WHERE code = @Code
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Branch>(
            new CommandDefinition(sql, new { Code = code }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                code AS Code,
                name AS Name,
                created_at AS CreatedAt
            FROM branches
            ORDER BY created_at DESC;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<Branch>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return items.AsList();
    }

    public async Task AddAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO branches (id, code, name, created_at)
            VALUES (@Id, @Code, @Name, @CreatedAt);
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, branch, cancellationToken: cancellationToken));
    }
}