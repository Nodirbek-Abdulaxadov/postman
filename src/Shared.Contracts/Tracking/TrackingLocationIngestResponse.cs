namespace PostalDeliverySystem.Shared.Contracts.Tracking;

public sealed class TrackingLocationIngestResponse
{
    public bool Accepted { get; set; }

    public string? IgnoredReason { get; set; }

    public DateTimeOffset? RecordedAt { get; set; }

    public double? DistanceMetersFromPrevious { get; set; }
}
