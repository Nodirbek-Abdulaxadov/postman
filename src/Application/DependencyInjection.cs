using Microsoft.Extensions.DependencyInjection;
using PostalDeliverySystem.Application.Auth;
using PostalDeliverySystem.Application.Branches;
using PostalDeliverySystem.Application.Courier;
using PostalDeliverySystem.Application.Orders;
using PostalDeliverySystem.Application.Tracking;
using PostalDeliverySystem.Application.Users;
using PostalDeliverySystem.Application.Workers;

namespace PostalDeliverySystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<ICourierOrderService, CourierOrderService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ITrackingAuthorizationService, TrackingAuthorizationService>();
        services.AddScoped<ITrackingService, TrackingService>();
        services.AddScoped<IUserService, UserService>();

        services.AddHostedService<RefreshTokenCleanupWorker>();
        services.AddHostedService<StaleCourierDetectorWorker>();

        return services;
    }
}