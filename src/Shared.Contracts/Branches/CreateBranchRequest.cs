namespace PostalDeliverySystem.Shared.Contracts.Branches;

public sealed class CreateBranchRequest
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}