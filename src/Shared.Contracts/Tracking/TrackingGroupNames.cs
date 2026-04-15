namespace PostalDeliverySystem.Shared.Contracts.Tracking;

public static class TrackingGroupNames
{
    public static string Order(Guid orderId) => $"order:{orderId}";

    public static string Branch(Guid branchId) => $"branch:{branchId}";

    public static string Courier(Guid courierId) => $"courier:{courierId}";
}
