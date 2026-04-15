using System.Data;
using System.Data.Common;
using System.Text;
using Dapper;
using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;
using PostalDeliverySystem.Infrastructure.Data;

namespace PostalDeliverySystem.Infrastructure.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OrderRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(
        Order order,
        OrderStatusHistory initialHistory,
        CancellationToken cancellationToken = default)
    {
        const string insertOrderSql = """
            INSERT INTO orders (
                id,
                order_code,
                customer_id,
                branch_id,
                courier_id,
                status,
                recipient_name,
                recipient_phone,
                address,
                lat,
                lng,
                assigned_at,
                delivered_at,
                created_at)
            VALUES (
                @Id,
                @OrderCode,
                @CustomerId,
                @BranchId,
                @CourierId,
                @Status,
                @RecipientName,
                @RecipientPhone,
                @Address,
                @Lat,
                @Lng,
                @AssignedAt,
                @DeliveredAt,
                @CreatedAt);
            """;

        const string insertHistorySql = """
            INSERT INTO order_status_history (
                order_id,
                from_status,
                to_status,
                changed_by_user_id,
                note,
                created_at)
            VALUES (
                @OrderId,
                @FromStatus,
                @ToStatus,
                @ChangedByUserId,
                @Note,
                @CreatedAt);
            """;

        using var connection = _connectionFactory.CreateConnection();
        var dbConnection = RequireDbConnection(connection);
        await dbConnection.OpenAsync(cancellationToken);
        await using var transaction = await dbConnection.BeginTransactionAsync(cancellationToken);

        try
        {
            await dbConnection.ExecuteAsync(
                new CommandDefinition(insertOrderSql, order, transaction, cancellationToken: cancellationToken));

            await dbConnection.ExecuteAsync(
                new CommandDefinition(insertHistorySql, initialHistory, transaction, cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                order_code AS OrderCode,
                customer_id AS CustomerId,
                branch_id AS BranchId,
                courier_id AS CourierId,
                status AS Status,
                recipient_name AS RecipientName,
                recipient_phone AS RecipientPhone,
                address AS Address,
                lat AS Lat,
                lng AS Lng,
                assigned_at AS AssignedAt,
                delivered_at AS DeliveredAt,
                created_at AS CreatedAt
            FROM orders
            WHERE id = @OrderId
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Order>(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken));
    }

    public async Task<Order?> GetByIdForCourierAsync(Guid orderId, Guid courierId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                order_code AS OrderCode,
                customer_id AS CustomerId,
                branch_id AS BranchId,
                courier_id AS CourierId,
                status AS Status,
                recipient_name AS RecipientName,
                recipient_phone AS RecipientPhone,
                address AS Address,
                lat AS Lat,
                lng AS Lng,
                assigned_at AS AssignedAt,
                delivered_at AS DeliveredAt,
                created_at AS CreatedAt
            FROM orders
            WHERE id = @OrderId AND courier_id = @CourierId
            LIMIT 1;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Order>(
            new CommandDefinition(
                sql,
                new
                {
                    OrderId = orderId,
                    CourierId = courierId
                },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Order>> SearchAsync(OrderSearchFilter filter, CancellationToken cancellationToken = default)
    {
        var sql = new StringBuilder(
            """
            SELECT
                id AS Id,
                order_code AS OrderCode,
                customer_id AS CustomerId,
                branch_id AS BranchId,
                courier_id AS CourierId,
                status AS Status,
                recipient_name AS RecipientName,
                recipient_phone AS RecipientPhone,
                address AS Address,
                lat AS Lat,
                lng AS Lng,
                assigned_at AS AssignedAt,
                delivered_at AS DeliveredAt,
                created_at AS CreatedAt
            FROM orders
            """
        );

        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (filter.BranchId.HasValue)
        {
            whereClauses.Add("branch_id = @BranchId");
            parameters.Add("BranchId", filter.BranchId.Value);
        }

        if (filter.CustomerId.HasValue)
        {
            whereClauses.Add("customer_id = @CustomerId");
            parameters.Add("CustomerId", filter.CustomerId.Value);
        }

        if (filter.CourierId.HasValue)
        {
            whereClauses.Add("courier_id = @CourierId");
            parameters.Add("CourierId", filter.CourierId.Value);
        }

        if (filter.Status.HasValue)
        {
            whereClauses.Add("status = @Status");
            parameters.Add("Status", filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.OrderCode))
        {
            whereClauses.Add("order_code ILIKE @OrderCode");
            parameters.Add("OrderCode", $"%{filter.OrderCode}%");
        }

        if (whereClauses.Count > 0)
        {
            sql.AppendLine("WHERE " + string.Join(" AND ", whereClauses));
        }

        sql.AppendLine("ORDER BY created_at DESC");
        sql.AppendLine("LIMIT @Limit;");
        parameters.Add("Limit", filter.Limit);

        using var connection = _connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<Order>(
            new CommandDefinition(sql.ToString(), parameters, cancellationToken: cancellationToken));

        return items.AsList();
    }

    public async Task<IReadOnlyList<Order>> GetAssignedToCourierAsync(
        Guid courierId,
        bool includeCompleted,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var sql = new StringBuilder(
            """
            SELECT
                id AS Id,
                order_code AS OrderCode,
                customer_id AS CustomerId,
                branch_id AS BranchId,
                courier_id AS CourierId,
                status AS Status,
                recipient_name AS RecipientName,
                recipient_phone AS RecipientPhone,
                address AS Address,
                lat AS Lat,
                lng AS Lng,
                assigned_at AS AssignedAt,
                delivered_at AS DeliveredAt,
                created_at AS CreatedAt
            FROM orders
            WHERE courier_id = @CourierId
            """
        );

        var parameters = new DynamicParameters();
        parameters.Add("CourierId", courierId);
        parameters.Add("Limit", limit);

        if (!includeCompleted)
        {
            sql.AppendLine("AND status IN @ActiveStatuses");
            parameters.Add("ActiveStatuses", new[]
            {
                OrderStatus.Assigned,
                OrderStatus.Accepted,
                OrderStatus.PickedUp,
                OrderStatus.OnTheWay
            });
        }

        sql.AppendLine("ORDER BY created_at DESC");
        sql.AppendLine("LIMIT @Limit;");

        using var connection = _connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<Order>(
            new CommandDefinition(sql.ToString(), parameters, cancellationToken: cancellationToken));

        return items.AsList();
    }

    public async Task<bool> UpdateDetailsIfCreatedAsync(Order order, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE orders
            SET
                recipient_name = @RecipientName,
                recipient_phone = @RecipientPhone,
                address = @Address,
                lat = @Lat,
                lng = @Lng
            WHERE id = @Id AND status = @ExpectedStatus;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    order.Id,
                    order.RecipientName,
                    order.RecipientPhone,
                    order.Address,
                    order.Lat,
                    order.Lng,
                    ExpectedStatus = OrderStatus.Created
                },
                cancellationToken: cancellationToken));

        return affected == 1;
    }

    public async Task<bool> AssignCourierIfCreatedAsync(
        Guid orderId,
        Guid courierId,
        Guid changedByUserId,
        string? note,
        DateTimeOffset assignedAt,
        CancellationToken cancellationToken = default)
    {
        const string updateOrderSql = """
            UPDATE orders
            SET
                courier_id = @CourierId,
                status = @AssignedStatus,
                assigned_at = @AssignedAt
            WHERE id = @OrderId AND status = @CreatedStatus;
            """;

        const string insertHistorySql = """
            INSERT INTO order_status_history (
                order_id,
                from_status,
                to_status,
                changed_by_user_id,
                note,
                created_at)
            VALUES (
                @OrderId,
                @FromStatus,
                @ToStatus,
                @ChangedByUserId,
                @Note,
                @CreatedAt);
            """;

        using var connection = _connectionFactory.CreateConnection();
        var dbConnection = RequireDbConnection(connection);
        await dbConnection.OpenAsync(cancellationToken);
        await using var transaction = await dbConnection.BeginTransactionAsync(cancellationToken);

        try
        {
            var affected = await dbConnection.ExecuteAsync(
                new CommandDefinition(
                    updateOrderSql,
                    new
                    {
                        OrderId = orderId,
                        CourierId = courierId,
                        AssignedStatus = OrderStatus.Assigned,
                        AssignedAt = assignedAt,
                        CreatedStatus = OrderStatus.Created
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            if (affected != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await dbConnection.ExecuteAsync(
                new CommandDefinition(
                    insertHistorySql,
                    new
                    {
                        OrderId = orderId,
                        FromStatus = OrderStatus.Created,
                        ToStatus = OrderStatus.Assigned,
                        ChangedByUserId = changedByUserId,
                        Note = note,
                        CreatedAt = assignedAt
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> TransitionStatusForCourierAsync(
        Guid orderId,
        Guid courierId,
        OrderStatus expectedStatus,
        OrderStatus nextStatus,
        Guid changedByUserId,
        string? note,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken = default)
    {
        const string updateOrderSql = """
            UPDATE orders
            SET
                status = @NextStatus,
                delivered_at = CASE
                    WHEN @NextStatus = @DeliveredStatus THEN @ChangedAt
                    ELSE delivered_at
                END
            WHERE id = @OrderId
              AND courier_id = @CourierId
              AND status = @ExpectedStatus;
            """;

        const string insertHistorySql = """
            INSERT INTO order_status_history (
                order_id,
                from_status,
                to_status,
                changed_by_user_id,
                note,
                created_at)
            VALUES (
                @OrderId,
                @FromStatus,
                @ToStatus,
                @ChangedByUserId,
                @Note,
                @CreatedAt);
            """;

        using var connection = _connectionFactory.CreateConnection();
        var dbConnection = RequireDbConnection(connection);
        await dbConnection.OpenAsync(cancellationToken);
        await using var transaction = await dbConnection.BeginTransactionAsync(cancellationToken);

        try
        {
            var affected = await dbConnection.ExecuteAsync(
                new CommandDefinition(
                    updateOrderSql,
                    new
                    {
                        OrderId = orderId,
                        CourierId = courierId,
                        ExpectedStatus = expectedStatus,
                        NextStatus = nextStatus,
                        DeliveredStatus = OrderStatus.Delivered,
                        ChangedAt = changedAt
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            if (affected != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await dbConnection.ExecuteAsync(
                new CommandDefinition(
                    insertHistorySql,
                    new
                    {
                        OrderId = orderId,
                        FromStatus = expectedStatus,
                        ToStatus = nextStatus,
                        ChangedByUserId = changedByUserId,
                        Note = note,
                        CreatedAt = changedAt
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<OrderStatusHistory>> GetStatusHistoryAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                order_id AS OrderId,
                from_status AS FromStatus,
                to_status AS ToStatus,
                changed_by_user_id AS ChangedByUserId,
                note AS Note,
                created_at AS CreatedAt
            FROM order_status_history
            WHERE order_id = @OrderId
            ORDER BY created_at DESC;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<OrderStatusHistory>(
            new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken));

        return items.AsList();
    }

    private static DbConnection RequireDbConnection(IDbConnection connection)
    {
        if (connection is not DbConnection dbConnection)
        {
            throw new InvalidOperationException("Configured connection does not support async database operations.");
        }

        return dbConnection;
    }
}