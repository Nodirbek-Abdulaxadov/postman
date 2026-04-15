using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Application.Abstractions.Time;
using PostalDeliverySystem.Domain.Entities;
using PostalDeliverySystem.Domain.Enums;

namespace PostalDeliverySystem.Application.Workers;

public sealed class StaleCourierDetectorWorker : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(2);

    private static readonly OrderStatus[] ActiveStatuses =
    [
        OrderStatus.Accepted,
        OrderStatus.PickedUp,
        OrderStatus.OnTheWay
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StaleCourierDetectorWorker> _logger;

    public StaleCourierDetectorWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<StaleCourierDetectorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StaleCourierDetectorWorker started.");

        // Delay initial run to allow app to warm up
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DetectStaleCouriersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StaleCourierDetectorWorker encountered an error.");
            }

            await Task.Delay(RunInterval, stoppingToken);
        }

        _logger.LogInformation("StaleCourierDetectorWorker stopped.");
    }

    /// <summary>Exposed for unit testing only — do not call from production code.</summary>
    internal Task RunDetectionForTestAsync(CancellationToken cancellationToken)
        => DetectStaleCouriersAsync(cancellationToken);

    private async Task DetectStaleCouriersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var trackingRepository = scope.ServiceProvider.GetRequiredService<ITrackingRepository>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var now = clock.UtcNow;
        var staleThresholdTime = now - StaleThreshold;

        // Collect all unique couriers on active deliveries
        var checkedCouriers = new HashSet<Guid>();

        foreach (var status in ActiveStatuses)
        {
            var orders = await orderRepository.SearchAsync(
                new OrderSearchFilter { Status = status, Limit = 500 },
                cancellationToken);

            foreach (var order in orders)
            {
                if (order.CourierId is null || !checkedCouriers.Add(order.CourierId.Value))
                {
                    continue;
                }

                CourierLocation? latestLocation;
                try
                {
                    latestLocation = await trackingRepository.GetLatestByCourierAsync(
                        order.CourierId.Value, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "StaleCourierDetector: could not read location for courier {CourierId}.",
                        order.CourierId.Value);
                    continue;
                }

                if (latestLocation is null || latestLocation.CreatedAt < staleThresholdTime)
                {
                    _logger.LogWarning(
                        "StaleCourier: courier {CourierId} on order {OrderId} ({Status}) has no location update since {LastSeen}.",
                        order.CourierId.Value,
                        order.Id,
                        status,
                        latestLocation?.CreatedAt.ToString("o") ?? "never");
                }
            }
        }
    }
}
