using Microsoft.AspNetCore.SignalR.Client;
using PostalDeliverySystem.Shared.Contracts.Tracking;

namespace PostalDeliverySystem.Shared.Client.Realtime;

public sealed class TrackingRealtimeClient : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private bool _disposed;

    public TrackingRealtimeClient(string apiBaseUrl, Func<string?> accessTokenProvider)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            throw new ArgumentException("API base URL is required.", nameof(apiBaseUrl));
        }

        if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var parsedBaseUrl))
        {
            throw new ArgumentException("API base URL must be absolute.", nameof(apiBaseUrl));
        }

        var hubUrl = new Uri(parsedBaseUrl, "/hubs/tracking");

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(accessTokenProvider());
            })
            .WithAutomaticReconnect(new[]
            {
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10)
            })
            .Build();

        _connection.Reconnecting += _ =>
        {
            SetConnectionStatus(RealtimeConnectionStatus.Reconnecting);
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            SetConnectionStatus(RealtimeConnectionStatus.Connected);
            return Task.CompletedTask;
        };

        _connection.Closed += _ =>
        {
            SetConnectionStatus(RealtimeConnectionStatus.Disconnected);
            return Task.CompletedTask;
        };

        _connection.On<TrackingLocationUpdateEvent>("location_update", payload =>
        {
            LastMessageAt = DateTimeOffset.UtcNow;
            LocationUpdated?.Invoke(payload);
        });

        _connection.On<TrackingStatusUpdateEvent>("status_update", payload =>
        {
            LastMessageAt = DateTimeOffset.UtcNow;
            StatusUpdated?.Invoke(payload);
        });
    }

    public RealtimeConnectionStatus ConnectionStatus { get; private set; } = RealtimeConnectionStatus.Disconnected;

    public DateTimeOffset? LastMessageAt { get; private set; }

    public event Action<RealtimeConnectionStatus>? ConnectionStatusChanged;

    public event Action<TrackingLocationUpdateEvent>? LocationUpdated;

    public event Action<TrackingStatusUpdateEvent>? StatusUpdated;

    public async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_connection.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting)
        {
            return;
        }

        SetConnectionStatus(RealtimeConnectionStatus.Connecting);
        await _connection.StartAsync(cancellationToken);
        SetConnectionStatus(RealtimeConnectionStatus.Connected);
    }

    public Task JoinOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return InvokeWithConnectionAsync("JoinOrder", orderId, cancellationToken);
    }

    public Task LeaveOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return InvokeWithoutThrowIfDisconnectedAsync("LeaveOrder", orderId, cancellationToken);
    }

    public Task JoinBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        return InvokeWithConnectionAsync("JoinBranch", branchId, cancellationToken);
    }

    public Task LeaveBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        return InvokeWithoutThrowIfDisconnectedAsync("LeaveBranch", branchId, cancellationToken);
    }

    public Task JoinCourierAsync(Guid courierId, CancellationToken cancellationToken = default)
    {
        return InvokeWithConnectionAsync("JoinCourier", courierId, cancellationToken);
    }

    public Task LeaveCourierAsync(Guid courierId, CancellationToken cancellationToken = default)
    {
        return InvokeWithoutThrowIfDisconnectedAsync("LeaveCourier", courierId, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            await _connection.DisposeAsync();
        }
        finally
        {
            SetConnectionStatus(RealtimeConnectionStatus.Disconnected);
        }
    }

    private async Task InvokeWithConnectionAsync(string methodName, Guid value, CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);
        await _connection.InvokeAsync(methodName, value, cancellationToken);
    }

    private async Task InvokeWithoutThrowIfDisconnectedAsync(string methodName, Guid value, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (_connection.State is not HubConnectionState.Connected)
        {
            return;
        }

        await _connection.InvokeAsync(methodName, value, cancellationToken);
    }

    private void SetConnectionStatus(RealtimeConnectionStatus next)
    {
        if (ConnectionStatus == next)
        {
            return;
        }

        ConnectionStatus = next;
        ConnectionStatusChanged?.Invoke(next);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TrackingRealtimeClient));
        }
    }
}
