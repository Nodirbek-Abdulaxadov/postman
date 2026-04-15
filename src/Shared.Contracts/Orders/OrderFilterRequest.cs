using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Shared.Contracts.Orders;

public sealed class OrderFilterRequest
{
    public Guid? BranchId { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? CourierId { get; set; }

    public OrderStatus? Status { get; set; }

    public string? OrderCode { get; set; }

    public int Limit { get; set; } = 100;
}