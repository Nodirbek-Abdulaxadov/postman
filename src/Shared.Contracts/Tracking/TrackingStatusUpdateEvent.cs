namespace PostalDeliverySystem.Shared.Contracts.Tracking;

public sealed class TrackingStatusUpdateEvent
{
    public Guid OrderId { get; set; }

    public Guid? CourierId { get; set; }

    public Guid BranchId { get; set; }

    public string Status { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public DateTimeOffset ChangedAt { get; set; }
}
