using Microsoft.Extensions.Logging;
using PostalDeliverySystem.CourierMobile.Services;

namespace PostalDeliverySystem.CourierMobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddSingleton(new ApiClientOptions());
		builder.Services.AddSingleton(sp =>
		{
			var options = sp.GetRequiredService<ApiClientOptions>();
			return new HttpClient
			{
				BaseAddress = new Uri(options.BaseUrl)
			};
		});
		builder.Services.AddSingleton<CourierSessionService>();
		builder.Services.AddSingleton<CourierApiClient>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
