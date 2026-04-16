namespace EnterpriseAgentOs.Api.Entities.Providers;

public sealed class ProviderKeyProtector
{
    private const string Purpose = "eaos.provider.api_key.v1";
    private readonly IDataProtector _protector;

    public ProviderKeyProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}
