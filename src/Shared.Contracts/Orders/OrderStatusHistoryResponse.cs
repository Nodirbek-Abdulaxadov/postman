using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Shared.Contracts.Orders;

public sealed class OrderStatusHistoryResponse
{
    public long Id { get; set; }

    public Guid OrderId { get; set; }

    public OrderStatus? FromStatus { get; set; }

    public OrderStatus ToStatus { get; set; }

    public Guid ChangedByUserId { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}