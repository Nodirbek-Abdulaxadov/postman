using PostalDeliverySystem.Domain.Entities;

namespace PostalDeliverySystem.Application.Abstractions.Security;

public interface IJwtTokenService
{
    AccessTokenResult GenerateAccessToken(User user, DateTimeOffset now);
}