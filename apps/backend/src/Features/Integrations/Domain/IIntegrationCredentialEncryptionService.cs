namespace OffceOs.Features.Integrations.Domain;

public interface IIntegrationCredentialEncryptionService
{
    Task<string> ProtectAsync(Dictionary<string, string> secret, CancellationToken ct = default);
    Task<Dictionary<string, string>> UnprotectAsync(string envelope, CancellationToken ct = default);
}
