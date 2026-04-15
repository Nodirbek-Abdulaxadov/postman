namespace PostalDeliverySystem.Shared.Contracts.Tracking;

public sealed class TrackingLocationUpdateEvent
{
    public Guid OrderId { get; set; }

    public Guid CourierId { get; set; }

    public Guid BranchId { get; set; }

    public double Lat { get; set; }

    public double Lng { get; set; }

    public DateTimeOffset RecordedAt { get; set; }
}
