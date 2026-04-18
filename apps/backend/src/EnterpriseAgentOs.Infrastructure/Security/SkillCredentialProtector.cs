namespace EnterpriseAgentOs.Infrastructure.Security;

public sealed class SkillCredentialProtector
{
    private const string Purpose = "eaos.skill.credentials.v1";
    private readonly IDataProtector _protector;

    public SkillCredentialProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}
