namespace PostalDeliverySystem.Domain.Entities;

public sealed class CourierLocation
{
    public long Id { get; set; }

    public Guid CourierId { get; set; }

    public Guid? OrderId { get; set; }

    public double Lat { get; set; }

    public double Lng { get; set; }

    public float? AccuracyMeters { get; set; }

    public float? SpeedMps { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}