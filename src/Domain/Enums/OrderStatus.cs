namespace PostalDeliverySystem.Domain.Enums;

public enum OrderStatus : short
{
    Created = 0,
    Assigned = 1,
    Accepted = 2,
    PickedUp = 3,
    OnTheWay = 4,
    Delivered = 5,
    Cancelled = 6
}