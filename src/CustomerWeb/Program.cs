using Microsoft.Extensions.Options;
using PostalDeliverySystem.CustomerWeb.Components;
using PostalDeliverySystem.CustomerWeb.Services;
using PostalDeliverySystem.Shared.Client.Realtime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services
    .AddOptions<ApiClientOptions>()
    .Bind(builder.Configuration.GetSection(ApiClientOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "API base URL must be a valid absolute URI.")
    .ValidateOnStart();

builder.Services.AddScoped<CustomerSession>();
builder.Services.AddHttpClient<CustomerApiClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<ApiClientOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});
builder.Services.AddScoped<TrackingRealtimeClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<ApiClientOptions>>().Value;
    var session = sp.GetRequiredService<CustomerSession>();
    return new TrackingRealtimeClient(options.BaseUrl, () => session.AccessToken);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
