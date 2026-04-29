using System.Security.Cryptography;
using System.Text;

namespace EnterpriseAgentOs.Domain.Features.Management;

public static class SessionTokenHasher
{
    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(bytes);
    }
}
