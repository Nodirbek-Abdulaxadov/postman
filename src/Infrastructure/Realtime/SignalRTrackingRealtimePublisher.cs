using Microsoft.AspNetCore.SignalR;
using PostalDeliverySystem.Application.Abstractions.Realtime;
using PostalDeliverySystem.Shared.Contracts.Tracking;

namespace PostalDeliverySystem.Infrastructure.Realtime;

public sealed class SignalRTrackingRealtimePublisher : ITrackingRealtimePublisher
{
    private readonly IHubContext<TrackingHub> _hubContext;

    public SignalRTrackingRealtimePublisher(IHubContext<TrackingHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PublishLocationUpdateAsync(TrackingLocationUpdateEvent payload, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            _hubContext.Clients.Group(TrackingGroupNames.Order(payload.OrderId))
                .SendAsync("location_update", payload, cancellationToken),

            _hubContext.Clients.Group(TrackingGroupNames.Branch(payload.BranchId))
                .SendAsync("location_update", payload, cancellationToken),

            _hubContext.Clients.Group(TrackingGroupNames.Courier(payload.CourierId))
                .SendAsync("location_update", payload, cancellationToken)
        };

        await Task.WhenAll(tasks);
    }

    public async Task PublishStatusUpdateAsync(TrackingStatusUpdateEvent payload, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            _hubContext.Clients.Group(TrackingGroupNames.Order(payload.OrderId))
                .SendAsync("status_update", payload, cancellationToken),

            _hubContext.Clients.Group(TrackingGroupNames.Branch(payload.BranchId))
                .SendAsync("status_update", payload, cancellationToken)
        };

        if (payload.CourierId.HasValue)
        {
            tasks.Add(_hubContext.Clients.Group(TrackingGroupNames.Courier(payload.CourierId.Value))
                .SendAsync("status_update", payload, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }
}
