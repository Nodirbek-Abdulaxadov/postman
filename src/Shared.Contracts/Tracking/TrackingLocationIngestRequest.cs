namespace PostalDeliverySystem.Shared.Contracts.Tracking;

public sealed class TrackingLocationIngestRequest
{
    public Guid CourierId { get; set; }

    public Guid OrderId { get; set; }

    public double Lat { get; set; }

    public double Lng { get; set; }

    public float? AccuracyMeters { get; set; }

    public float? SpeedMps { get; set; }

    public DateTimeOffset DeviceTime { get; set; }
}
