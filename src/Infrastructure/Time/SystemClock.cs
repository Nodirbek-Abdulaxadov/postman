using PostalDeliverySystem.Application.Abstractions.Time;

namespace PostalDeliverySystem.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}