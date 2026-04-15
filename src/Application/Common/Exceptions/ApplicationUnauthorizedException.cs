namespace PostalDeliverySystem.Application.Common.Exceptions;

public sealed class ApplicationUnauthorizedException(string message) : Exception(message);