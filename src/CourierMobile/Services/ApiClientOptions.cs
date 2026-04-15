namespace PostalDeliverySystem.CourierMobile.Services;

public sealed class ApiClientOptions
{
    public string BaseUrl { get; }

    public ApiClientOptions()
    {
        BaseUrl = DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5291/"
            : "http://localhost:5291/";
    }
}