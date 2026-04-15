using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Domain.Entities;

public sealed class Order
{
    public Guid Id { get; set; }

    public string OrderCode { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public Guid BranchId { get; set; }

    public Guid? CourierId { get; set; }

    public OrderStatus Status { get; set; }

    public string RecipientName { get; set; } = string.Empty;

    public string RecipientPhone { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public double Lat { get; set; }

    public double Lng { get; set; }

    public DateTimeOffset? AssignedAt { get; set; }

    public DateTimeOffset? DeliveredAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}