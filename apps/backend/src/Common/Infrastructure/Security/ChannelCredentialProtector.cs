namespace EnterpriseAgentOs.Infrastructure.Common.Security;

public sealed class ChannelCredentialProtector
{
    private const string Purpose = "eaos.channel.creds.v1";
    private readonly IDataProtector _dataProtector;

    public ChannelCredentialProtector(IDataProtectionProvider provider)
    {
        _dataProtector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _dataProtector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _dataProtector.Unprotect(ciphertext);
}
