namespace PostalDeliverySystem.Shared.Contracts.Orders;

public sealed class CreateOrderRequest
{
    public Guid? BranchId { get; set; }

    public Guid CustomerId { get; set; }

    public string RecipientName { get; set; } = string.Empty;

    public string RecipientPhone { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public double Lat { get; set; }

    public double Lng { get; set; }

    public string? Note { get; set; }
}