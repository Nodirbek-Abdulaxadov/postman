using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PostalDeliverySystem.Shared.Contracts.Auth;
using PostalDeliverySystem.Shared.Contracts.Courier;
using PostalDeliverySystem.Shared.Contracts.Orders;

namespace PostalDeliverySystem.CourierMobile.Services;

public sealed class CourierApiClient
{
    private readonly HttpClient _httpClient;
    private readonly CourierSessionService _session;

    public CourierApiClient(HttpClient httpClient, CourierSessionService session)
    {
        _httpClient = httpClient;
        _session = session;
    }

    public async Task<AuthTokensResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<AuthTokensResponse>(cancellationToken);
        return payload ?? throw new InvalidOperationException("Empty login response from API.");
    }

    public async Task<AuthTokensResponse> RefreshWithRawTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken },
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<AuthTokensResponse>(cancellationToken);
        return payload ?? throw new InvalidOperationException("Empty refresh response from API.");
    }

    public async Task<IReadOnlyList<OrderResponse>> GetAssignedOrdersAsync(
        bool includeCompleted,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"api/courier/orders?includeCompleted={includeCompleted.ToString().ToLowerInvariant()}&limit={Math.Clamp(limit, 1, 100)}");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<OrderResponse>>(cancellationToken);
        return payload ?? Array.Empty<OrderResponse>();
    }

    public async Task<OrderResponse> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var request = CreateAuthorizedRequest(HttpMethod.Get, $"api/courier/orders/{orderId}");
        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<OrderResponse>(cancellationToken);
        return payload ?? throw new InvalidOperationException("Empty order detail response from API.");
    }

    public Task<OrderResponse> AcceptAsync(Guid orderId, string? note, CancellationToken cancellationToken = default)
    {
        return SendActionWithRetryAsync($"api/courier/orders/{orderId}/accept", note, cancellationToken);
    }

    public Task<OrderResponse> PickUpAsync(Guid orderId, string? note, CancellationToken cancellationToken = default)
    {
        return SendActionWithRetryAsync($"api/courier/orders/{orderId}/pickup", note, cancellationToken);
    }

    public Task<OrderResponse> StartOnTheWayAsync(Guid orderId, string? note, CancellationToken cancellationToken = default)
    {
        return SendActionWithRetryAsync($"api/courier/orders/{orderId}/on-the-way", note, cancellationToken);
    }

    public Task<OrderResponse> DeliverAsync(Guid orderId, string? note, CancellationToken cancellationToken = default)
    {
        return SendActionWithRetryAsync($"api/courier/orders/{orderId}/deliver", note, cancellationToken);
    }

    private async Task<OrderResponse> SendActionWithRetryAsync(string path, string? note, CancellationToken cancellationToken)
    {
        const int maxAttempts = 2;
        Exception? lastError = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var request = CreateAuthorizedRequest(HttpMethod.Post, path, new CourierOrderActionRequest { Note = note });
                var response = await _httpClient.SendAsync(request, cancellationToken);
                await EnsureSuccessAsync(response, cancellationToken);

                var payload = await response.Content.ReadFromJsonAsync<OrderResponse>(cancellationToken);
                return payload ?? throw new InvalidOperationException("Empty status transition response from API.");
            }
            catch (HttpRequestException ex)
            {
                lastError = ex;
                if (attempt == maxAttempts)
                {
                    break;
                }

                await Task.Delay(500, cancellationToken);
            }
        }

        throw new InvalidOperationException("Network error while sending action. Please retry.", lastError);
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string path, object? body = null)
    {
        if (!_session.IsAuthenticated)
        {
            throw new InvalidOperationException("Session is not authenticated.");
        }

        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string message;
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<ApiErrorPayload>(cancellationToken);
            message = payload?.Error ?? string.Empty;
        }
        catch
        {
            message = string.Empty;
        }

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

    private sealed class ApiErrorPayload
    {
        public string? Error { get; set; }
    }
}