namespace PostalDeliverySystem.Shared.Contracts.Orders;

public sealed class UpdateOrderRequest
{
    public string RecipientName { get; set; } = string.Empty;

    public string RecipientPhone { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public double Lat { get; set; }

    public double Lng { get; set; }
}