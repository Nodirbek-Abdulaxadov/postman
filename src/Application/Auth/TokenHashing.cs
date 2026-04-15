using System.Security.Cryptography;
using System.Text;

namespace PostalDeliverySystem.Application.Auth;

internal static class TokenHashing
{
    public static string ComputeSha256(string rawToken)
    {
        var payload = Encoding.UTF8.GetBytes(rawToken);
        var digest = SHA256.HashData(payload);
        return Convert.ToHexString(digest);
    }
}