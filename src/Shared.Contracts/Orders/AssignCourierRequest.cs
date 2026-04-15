namespace PostalDeliverySystem.Shared.Contracts.Orders;

public sealed class AssignCourierRequest
{
    public Guid CourierId { get; set; }

    public string? Note { get; set; }
}