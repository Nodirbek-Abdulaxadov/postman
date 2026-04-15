namespace PostalDeliverySystem.Shared.Contracts.Courier;

public sealed class CourierAssignedOrderFilterRequest
{
    public bool IncludeCompleted { get; set; }

    public int Limit { get; set; } = 50;
}