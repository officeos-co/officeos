namespace OffceOs.Configuration;

public sealed class IntegrationCredentialEncryptionConfig
{
    public string Provider { get; init; } = "data_protection";
    public string? VaultAddress { get; init; }
    public string? VaultToken { get; init; }
    public string VaultTransitMount { get; init; } = "transit";
    public string VaultKeyName { get; init; } = "eaos-integrations";
}
