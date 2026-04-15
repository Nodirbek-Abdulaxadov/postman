namespace PostalDeliverySystem.Application.Abstractions.Security;

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);