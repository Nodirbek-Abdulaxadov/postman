using System.Data;

namespace PostalDeliverySystem.Infrastructure.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}