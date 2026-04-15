namespace PostalDeliverySystem.Shared.Contracts.Branches;

public sealed class BranchResponse
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}