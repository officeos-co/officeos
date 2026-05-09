namespace OffceOs.Infrastructure.Common.Security;

public sealed class CredentialProtector
{
    private readonly IDataProtector _dataProtector;

    public CredentialProtector(IDataProtectionProvider provider)
    {
        _dataProtector = provider.CreateProtector("eaos.credentials.v1");
    }

    public string Protect(Dictionary<string, string> credentials)
    {
        var json = JsonSerializer.Serialize(credentials);
        return _dataProtector.Protect(json);
    }

    public Dictionary<string, string> Unprotect(string encrypted)
    {
        var json = _dataProtector.Unprotect(encrypted);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
    }
}
