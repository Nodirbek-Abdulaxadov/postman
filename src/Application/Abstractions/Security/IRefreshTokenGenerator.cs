namespace PostalDeliverySystem.Application.Abstractions.Security;

public interface IRefreshTokenGenerator
{
    string Generate();
}