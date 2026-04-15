namespace PostalDeliverySystem.Domain.Entities;

public sealed class Branch
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}