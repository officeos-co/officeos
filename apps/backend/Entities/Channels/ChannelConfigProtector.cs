namespace EnterpriseAgentOs.Api.Entities.Channels;

public sealed class ChannelConfigProtector
{
    private const string Purpose = "eaos.channel.config.v1";
    private readonly IDataProtector _protector;

    public ChannelConfigProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}
