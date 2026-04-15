using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Application.Abstractions.Time;

namespace PostalDeliverySystem.Application.Workers;

public sealed class RefreshTokenCleanupWorker : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan RevokedRetentionWindow = TimeSpan.FromDays(7);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefreshTokenCleanupWorker> _logger;

    public RefreshTokenCleanupWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<RefreshTokenCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RefreshTokenCleanupWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RefreshTokenCleanupWorker encountered an error during cleanup.");
            }

            await Task.Delay(RunInterval, stoppingToken);
        }

        _logger.LogInformation("RefreshTokenCleanupWorker stopped.");
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var tokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var now = clock.UtcNow;
        var revokedBefore = now - RevokedRetentionWindow;

        var deleted = await tokenRepository.DeleteExpiredAsync(now, revokedBefore, cancellationToken);

        if (deleted > 0)
        {
            _logger.LogInformation(
                "RefreshTokenCleanup: deleted {Count} expired or revoked refresh tokens at {UtcNow}.",
                deleted, now);
        }
    }
}
