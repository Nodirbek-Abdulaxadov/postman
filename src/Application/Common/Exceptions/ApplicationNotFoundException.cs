namespace PostalDeliverySystem.Application.Common.Exceptions;

public sealed class ApplicationNotFoundException(string message) : Exception(message);