using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PostalDeliverySystem.Tests;

/// <summary>
/// HTTP-level smoke tests that verify the API pipeline is wired correctly.
/// These tests start the real application host in-process via WebApplicationFactory.
/// No database or Redis connection is required — tests either bypass infrastructure
/// (liveness probe, rate limiter) or are rejected by auth middleware before any DB call.
/// </summary>
public sealed class IntegrationSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationSmokeTests(WebApplicationFactory<Program> factory)
    {
        // Default appsettings.json has all required options populated, so
        // ValidateOnStart passes. Individual tests don't reach the database.
        _factory = factory;
    }

    // ──────────────────────────────────────────────
    // Health probes
    // ──────────────────────────────────────────────

    [Fact]
    public async Task HealthLive_Returns200_WithoutDatabase()
    {
        // /health/live uses Predicate = _ => false, so it skips all health checks
        // and returns Healthy regardless of DB / Redis availability.
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ──────────────────────────────────────────────
    // Authentication / authorization pipeline
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetOrders_Returns401_WhenNoToken()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_Returns401_WhenNoToken()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ──────────────────────────────────────────────
    // Rate limiting
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Login_Returns429_AfterRateLimitExceeded()
    {
        // Auth policy: 10 requests/minute per IP.
        // WebApplicationFactory sets RemoteIpAddress to null → partition key "unknown".
        // Send 10 requests to exhaust the permit, the 11th must be rejected.
        var client = _factory.CreateClient();

        var body = new StringContent("{}", Encoding.UTF8, "application/json");

        for (var i = 0; i < 10; i++)
        {
            // Responses can be anything (400, 422, etc.) — we only care they are NOT 429.
            var interim = await client.PostAsync("/api/auth/login", body);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, interim.StatusCode);
        }

        // 11th request must be rate-limited.
        var last = await client.PostAsync("/api/auth/login", body);
        Assert.Equal(HttpStatusCode.TooManyRequests, last.StatusCode);
    }

    [Fact]
    public async Task Login_RateLimitResponse_IncludesRetryAfterHeader()
    {
        var client = _factory.CreateClient();
        var body = new StringContent("{}", Encoding.UTF8, "application/json");

        // Exhaust the window.
        for (var i = 0; i < 10; i++)
            await client.PostAsync("/api/auth/login", body);

        var response = await client.PostAsync("/api/auth/login", body);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.True(response.Headers.Contains("Retry-After"), "Retry-After header must be present on 429 responses.");
    }

    // ──────────────────────────────────────────────
    // CorrelationId middleware
    // ──────────────────────────────────────────────

    [Fact]
    public async Task AnyRequest_ResponseContainsCorrelationIdHeader()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.True(
            response.Headers.Contains("X-Correlation-Id"),
            "Every response must echo back a X-Correlation-Id header.");
    }

    [Fact]
    public async Task Request_WithCorrelationIdHeader_EchosTheSameValue()
    {
        var client = _factory.CreateClient();
        var correlationId = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);

        var response = await client.GetAsync("/health/live");

        Assert.Equal(
            correlationId,
            response.Headers.GetValues("X-Correlation-Id").FirstOrDefault());
    }
}
