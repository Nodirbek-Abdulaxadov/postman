using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Application.Abstractions.Persistence;

public sealed class OrderSearchFilter
{
    public Guid? BranchId { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? CourierId { get; set; }

    public OrderStatus? Status { get; set; }

    public string? OrderCode { get; set; }

    public int Limit { get; set; } = 100;
}