namespace PostalDeliverySystem.AdminWeb.Services;

public sealed class ApiClientOptions
{
    public const string SectionName = "ApiClient";

    public string BaseUrl { get; set; } = "http://localhost:5291";
}