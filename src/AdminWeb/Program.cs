using Microsoft.Extensions.Options;
using PostalDeliverySystem.AdminWeb.Components;
using PostalDeliverySystem.AdminWeb.Services;
using PostalDeliverySystem.Shared.Client.Realtime;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services
    .AddOptions<ApiClientOptions>()
    .Bind(builder.Configuration.GetSection(ApiClientOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "Admin API base URL must be a valid absolute URI.")
    .ValidateOnStart();

builder.Services.AddScoped<AdminSession>();
builder.Services.AddHttpClient<AdminApiClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<ApiClientOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});
builder.Services.AddScoped<TrackingRealtimeClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<ApiClientOptions>>().Value;
    var session = sp.GetRequiredService<AdminSession>();
    return new TrackingRealtimeClient(options.BaseUrl, () => session.AccessToken);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
