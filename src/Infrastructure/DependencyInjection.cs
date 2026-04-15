using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PostalDeliverySystem.Application.Abstractions.Persistence;
using PostalDeliverySystem.Application.Abstractions.Realtime;
using PostalDeliverySystem.Application.Abstractions.Security;
using PostalDeliverySystem.Application.Abstractions.Time;
using PostalDeliverySystem.Infrastructure.Data;
using PostalDeliverySystem.Infrastructure.Options;
using PostalDeliverySystem.Infrastructure.Realtime;
using PostalDeliverySystem.Infrastructure.Repositories;
using PostalDeliverySystem.Infrastructure.Security;
using PostalDeliverySystem.Infrastructure.Time;
using StackExchange.Redis;

namespace PostalDeliverySystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "Database connection string is required.")
            .ValidateOnStart();

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "JWT issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "JWT audience is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey), "JWT signing key is required.")
            .Validate(options => options.AccessTokenMinutes > 0, "JWT access token lifetime must be greater than zero.")
            .ValidateOnStart();

        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();

        var redisConnectionString = configuration.GetSection("Redis").GetValue<string>("ConnectionString");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            redisConnectionString = "localhost:6379";
        }

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(redisConnectionString);
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 3;
            options.ReconnectRetryPolicy = new ExponentialRetry(5000);

            return ConnectionMultiplexer.Connect(options);
        });

        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ITrackingRepository, TrackingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddSingleton<ITrackingLocationCache, RedisTrackingLocationCache>();
        services.AddScoped<ITrackingRealtimePublisher, SignalRTrackingRealtimePublisher>();

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}