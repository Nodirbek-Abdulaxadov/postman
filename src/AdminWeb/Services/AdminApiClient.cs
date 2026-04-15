using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PostalDeliverySystem.Shared.Contracts.Auth;
using PostalDeliverySystem.Shared.Contracts.Orders;
using PostalDeliverySystem.Shared.Contracts.Tracking;
using PostalDeliverySystem.Shared.Contracts.Users;

namespace PostalDeliverySystem.AdminWeb.Services;

public sealed class AdminApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AdminSession _session;

    public AdminApiClient(HttpClient httpClient, AdminSession session)
    {
        _httpClient = httpClient;
        _session = session;
    }

    public async Task<AuthTokensResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<AuthTokensResponse>(cancellationToken);
        return payload ?? throw new InvalidOperationException("API returned an empty login response.");
    }

    public async Task<IReadOnlyList<UserResponse>> GetUsersAsync(
        UserFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["branchId"] = request.BranchId?.ToString(),
            ["role"] = request.Role?.ToString(),
            ["limit"] = request.Limit.ToString()
        });

        var httpRequest = CreateAuthorizedRequest(HttpMethod.Get, $"api/users{query}");
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<UserResponse>>(cancellationToken);
        return payload ?? Array.Empty<UserResponse>();
    }

    public async Task<IReadOnlyList<OrderResponse>> GetOrdersAsync(
        OrderFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["branchId"] = request.BranchId?.ToString(),
            ["customerId"] = request.CustomerId?.ToString(),
            ["courierId"] = request.CourierId?.ToString(),
            ["status"] = request.Status?.ToString(),
            ["orderCode"] = request.OrderCode,
            ["limit"] = request.Limit.ToString()
        });

        var httpRequest = CreateAuthorizedRequest(HttpMethod.Get, $"api/orders{query}");
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<OrderResponse>>(cancellationToken);
        return payload ?? Array.Empty<OrderResponse>();
    }

    public async Task<OrderResponse> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var httpRequest = CreateAuthorizedRequest(HttpMethod.Post, "api/orders", request);
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<OrderResponse>(cancellationToken);
        return payload ?? throw new InvalidOperationException("API returned an empty order creation response.");
    }

    public async Task<OrderResponse> AssignOrderAsync(
        Guid orderId,
        AssignCourierRequest request,
        CancellationToken cancellationToken = default)
    {
        var httpRequest = CreateAuthorizedRequest(HttpMethod.Post, $"api/orders/{orderId}/assign", request);
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<OrderResponse>(cancellationToken);
        return payload ?? throw new InvalidOperationException("API returned an empty assignment response.");
    }

    public async Task<IReadOnlyList<OrderStatusHistoryResponse>> GetOrderHistoryAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var httpRequest = CreateAuthorizedRequest(HttpMethod.Get, $"api/orders/{orderId}/history");
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<OrderStatusHistoryResponse>>(cancellationToken);
        return payload ?? Array.Empty<OrderStatusHistoryResponse>();
    }

    public async Task<TrackingLocationResponse?> GetLatestOrderLocationAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var httpRequest = CreateAuthorizedRequest(HttpMethod.Get, $"api/tracking/orders/{orderId}/latest-location");
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<TrackingLocationResponse>(cancellationToken);
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string path, object? payload = null)
    {
        if (!_session.IsAuthenticated)
        {
            throw new InvalidOperationException("You are not logged in.");
        }

        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);

        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(message))
        {
            message = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Unauthorized request.",
                HttpStatusCode.Forbidden => "Forbidden request.",
                HttpStatusCode.NotFound => "Resource not found.",
                HttpStatusCode.Conflict => "Conflict while processing request.",
                _ => "Unexpected API error."
            };
        }

        throw new InvalidOperationException(message);
    }

    private static string BuildQuery(IReadOnlyDictionary<string, string?> parameters)
    {
        var pairs = parameters
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}")
            .ToArray();

        return pairs.Length == 0 ? string.Empty : "?" + string.Join("&", pairs);
    }
}