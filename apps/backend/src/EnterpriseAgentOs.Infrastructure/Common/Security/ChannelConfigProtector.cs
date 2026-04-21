namespace EnterpriseAgentOs.Infrastructure.Common.Security;

public sealed class ChannelConfigProtector
{
    private const string Purpose = "eaos.channel.config.v1";
    private readonly IDataProtector _dataProtector;

    public ChannelConfigProtector(IDataProtectionProvider provider)
    {
        _dataProtector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _dataProtector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _dataProtector.Unprotect(ciphertext);
}
