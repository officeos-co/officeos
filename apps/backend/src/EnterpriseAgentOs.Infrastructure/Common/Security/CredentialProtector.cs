using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace EnterpriseAgentOs.Infrastructure.Common.Security;

public sealed class CredentialProtector
{
    private readonly IDataProtector _protector;

    public CredentialProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("eaos.credentials.v1");
    }

    public string Protect(Dictionary<string, string> credentials)
    {
        var json = JsonSerializer.Serialize(credentials);
        return _protector.Protect(json);
    }

    public Dictionary<string, string> Unprotect(string encrypted)
    {
        var json = _protector.Unprotect(encrypted);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
    }
}
