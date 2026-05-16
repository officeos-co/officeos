using OffceOs.Configuration;
using OffceOs.Features.Integrations.Domain;
namespace OffceOs.Features.Integrations.Infrastructure;

internal sealed class IntegrationCredentialEncryptionService : IIntegrationCredentialEncryptionService
{
    private const string Purpose = "eaos.integrations.credentials.v1";
    private readonly IDataProtector _dataProtector;
    private readonly IntegrationCredentialEncryptionConfig _integrationCredentialEncryptionConfig;
    private readonly IHttpClientFactory _httpClientFactory;

    public IntegrationCredentialEncryptionService(
        IDataProtectionProvider dataProtectionProvider,
        IntegrationCredentialEncryptionConfig integrationCredentialEncryptionConfig,
        IHttpClientFactory httpClientFactory)
    {
        _dataProtector = dataProtectionProvider.CreateProtector(Purpose);
        _integrationCredentialEncryptionConfig = integrationCredentialEncryptionConfig;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> ProtectAsync(Dictionary<string, string> secret, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(secret);
        if (UseVaultTransit)
        {
            var ciphertext = await VaultEncryptAsync(json, ct);
            return JsonSerializer.Serialize(new IntegrationCredentialSecretResponse(
                "vault_transit",
                _integrationCredentialEncryptionConfig.VaultTransitMount,
                _integrationCredentialEncryptionConfig.VaultKeyName,
                ciphertext));
        }

        return JsonSerializer.Serialize(new IntegrationCredentialSecretResponse(
            "data_protection",
            null,
            null,
            _dataProtector.Protect(json)));
    }

    public async Task<Dictionary<string, string>> UnprotectAsync(string envelope, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(envelope))
            return new();

        IntegrationCredentialSecretResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<IntegrationCredentialSecretResponse>(envelope);
        }
        catch (JsonException)
        {
            var legacyJson = _dataProtector.Unprotect(envelope);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(legacyJson) ?? new();
        }

        if (parsed is null)
            return new();

        var json = parsed.Provider switch
        {
            "vault_transit" => await VaultDecryptAsync(parsed.Ciphertext, ct),
            "data_protection" => _dataProtector.Unprotect(parsed.Ciphertext),
            _ => throw new InvalidOperationException($"Unsupported integration credential envelope provider '{parsed.Provider}'."),
        };

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
    }

    private bool UseVaultTransit =>
        string.Equals(_integrationCredentialEncryptionConfig.Provider, "vault_transit", StringComparison.OrdinalIgnoreCase);

    private async Task<string> VaultEncryptAsync(string plaintext, CancellationToken ct)
    {
        var response = await SendVaultTransitAsync(
            "encrypt",
            new { plaintext = Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext)) },
            ct);
        return response.GetProperty("data").GetProperty("ciphertext").GetString()
            ?? throw new InvalidOperationException("Vault Transit encrypt response did not include ciphertext.");
    }

    private async Task<string> VaultDecryptAsync(string ciphertext, CancellationToken ct)
    {
        var response = await SendVaultTransitAsync("decrypt", new { ciphertext }, ct);
        var plaintext = response.GetProperty("data").GetProperty("plaintext").GetString()
            ?? throw new InvalidOperationException("Vault Transit decrypt response did not include plaintext.");
        return Encoding.UTF8.GetString(Convert.FromBase64String(plaintext));
    }

    private async Task<JsonElement> SendVaultTransitAsync(string operation, object payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_integrationCredentialEncryptionConfig.VaultAddress)
            || string.IsNullOrWhiteSpace(_integrationCredentialEncryptionConfig.VaultToken))
        {
            throw new InvalidOperationException("Vault Transit integration credential encryption is not fully configured.");
        }

        var client = _httpClientFactory.CreateClient("vault-transit");
        var endpoint = new Uri(
            new Uri(_integrationCredentialEncryptionConfig.VaultAddress.TrimEnd('/') + "/"),
            $"v1/{_integrationCredentialEncryptionConfig.VaultTransitMount}/encrypt/{_integrationCredentialEncryptionConfig.VaultKeyName}");
        if (operation == "decrypt")
        {
            endpoint = new Uri(
                new Uri(_integrationCredentialEncryptionConfig.VaultAddress.TrimEnd('/') + "/"),
                $"v1/{_integrationCredentialEncryptionConfig.VaultTransitMount}/decrypt/{_integrationCredentialEncryptionConfig.VaultKeyName}");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add("X-Vault-Token", _integrationCredentialEncryptionConfig.VaultToken);

        var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Vault Transit {operation} failed: {body}");

        return JsonSerializer.Deserialize<JsonElement>(body);
    }
}

internal sealed record IntegrationCredentialSecretResponse(
    string Provider,
    string? Mount,
    string? KeyName,
    string Ciphertext);
