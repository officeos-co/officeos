namespace EnterpriseAgentOs.Infrastructure.Security;

public sealed class AgentBackendTokenProtector
{
    private const string Purpose = "eaos.agent.backend_token.v1";
    private readonly IDataProtector _protector;

    public AgentBackendTokenProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}
