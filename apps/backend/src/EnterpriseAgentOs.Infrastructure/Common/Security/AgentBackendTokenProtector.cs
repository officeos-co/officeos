namespace EnterpriseAgentOs.Infrastructure.Common.Security;

internal sealed class AgentBackendTokenProtector
{
    private const string Purpose = "eaos.agent.backend_token.v1";
    private readonly IDataProtector _dataProtector;

    public AgentBackendTokenProtector(IDataProtectionProvider provider)
    {
        _dataProtector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _dataProtector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _dataProtector.Unprotect(ciphertext);
}
