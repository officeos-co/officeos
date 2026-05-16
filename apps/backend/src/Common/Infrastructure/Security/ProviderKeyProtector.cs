namespace OffceOs.Common.Infrastructure.Security;

public sealed class ProviderKeyProtector
{
    private const string Purpose = "eaos.provider.api_key.v1";
    private readonly IDataProtector _dataProtector;

    public ProviderKeyProtector(IDataProtectionProvider provider)
    {
        _dataProtector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _dataProtector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _dataProtector.Unprotect(ciphertext);
}
