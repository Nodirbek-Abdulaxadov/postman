namespace PostalDeliverySystem.Application.Common.Exceptions;

public sealed class ApplicationConflictException(string message) : Exception(message);