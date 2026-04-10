using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.Skills;

public sealed class SkillService : ISkillService
{
    private readonly ISkillCredentialRepository _repository;
    private readonly SkillCredentialProtector _protector;

    public SkillService(
        ISkillCredentialRepository repository,
        SkillCredentialProtector protector)
    {
        _repository = repository;
        _protector = protector;
    }

    public async Task<IReadOnlyList<SkillDto>> ListAsync(CancellationToken ct = default)
    {
        var rows = (await _repository.ListAsync(ct))
            .ToDictionary(r => r.SkillName, StringComparer.OrdinalIgnoreCase);
        return SkillManifests.All.Values
            .Select(m =>
            {
                rows.TryGetValue(m.Name, out var row);
                return ToDto(m, row);
            })
            .ToList();
    }

    public async Task<SkillDto?> GetAsync(string name, CancellationToken ct = default)
    {
        var manifest = SkillManifests.For(name);
        if (manifest is null) return null;
        var row = await _repository.GetByNameAsync(manifest.Name, ct);
        return ToDto(manifest, row);
    }

    public async Task<SkillDto?> InstallAsync(string name, CancellationToken ct = default)
    {
        var manifest = SkillManifests.For(name);
        if (manifest is null) return null;
        var row = await _repository.UpsertAsync(manifest.Name, enabled: true, encryptedCredentials: null, ct);
        return ToDto(manifest, row);
    }

    public async Task<SkillDto?> UninstallAsync(string name, CancellationToken ct = default)
    {
        var manifest = SkillManifests.For(name);
        if (manifest is null) return null;
        var row = await _repository.UpsertAsync(manifest.Name, enabled: false, encryptedCredentials: null, ct);
        return ToDto(manifest, row);
    }

    public async Task<SkillDto?> PutCredentialsAsync(
        string name,
        IReadOnlyDictionary<string, string> credentials,
        CancellationToken ct = default)
    {
        var manifest = SkillManifests.For(name);
        if (manifest is null) return null;

        // Validate required fields.
        var missing = manifest.CredentialFields
            .Where(f => f.Required && string.IsNullOrWhiteSpace(GetField(credentials, f.Key)))
            .Select(f => f.Key)
            .ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing required credential fields for skill '{manifest.Name}': {string.Join(", ", missing)}");
        }

        // Only keep fields the manifest knows about.
        var known = manifest.CredentialFields.Select(f => f.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var filtered = credentials
            .Where(kv => known.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var json = JsonSerializer.Serialize(filtered);
        var ciphertext = _protector.Protect(json);

        var row = await _repository.UpsertAsync(manifest.Name, enabled: null, encryptedCredentials: ciphertext, ct);
        return ToDto(manifest, row);
    }

    public async Task<IReadOnlyDictionary<string, string>?> GetDecryptedCredentialsAsync(
        string name,
        CancellationToken ct = default)
    {
        var manifest = SkillManifests.For(name);
        if (manifest is null) return null;
        var row = await _repository.GetByNameAsync(manifest.Name, ct);
        if (row?.Enabled != true || string.IsNullOrEmpty(row.EncryptedCredentials))
        {
            return null;
        }
        var plaintext = _protector.Unprotect(row.EncryptedCredentials);
        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(plaintext);
        return parsed;
    }

    public async Task<CapabilitiesResponse> ListCapabilitiesAsync(CancellationToken ct = default)
    {
        var rows = (await _repository.ListAsync(ct))
            .ToDictionary(r => r.SkillName, StringComparer.OrdinalIgnoreCase);
        var caps = new List<CapabilityDto>();
        var docs = new List<SkillDocDto>();

        foreach (var manifest in SkillManifests.AllWithDocs.Values)
        {
            if (!rows.TryGetValue(manifest.Name, out var row)) continue;
            if (!row.Enabled || string.IsNullOrEmpty(row.EncryptedCredentials)) continue;

            foreach (var tool in manifest.LlmTools)
            {
                var dotIndex = tool.Name.IndexOf('.');
                var action = dotIndex > 0 ? tool.Name[(dotIndex + 1)..] : tool.Name;
                caps.Add(new CapabilityDto(
                    Skill: manifest.Name,
                    Name: tool.Name,
                    Description: tool.Description,
                    Parameters: tool.Parameters,
                    Route: $"/api/agents/me/skills/{manifest.Name}/{action}"));
            }

            if (manifest.Doc is not null)
            {
                docs.Add(new SkillDocDto(manifest.Name, manifest.Doc));
            }
        }
        return new CapabilitiesResponse(caps, docs);
    }

    private static SkillDto ToDto(SkillManifest manifest, SkillCredentialRecord? row) =>
        new(
            Name: manifest.Name,
            Title: manifest.Title,
            Description: manifest.Description,
            Emoji: manifest.Emoji,
            Installed: row?.Enabled == true,
            Configured: !string.IsNullOrEmpty(row?.EncryptedCredentials),
            CredentialFields: manifest.CredentialFields,
            LlmTools: manifest.LlmTools
                .Select(t => new LlmToolDto(t.Name, t.Description, t.Parameters))
                .ToList());

    private static string? GetField(IReadOnlyDictionary<string, string> creds, string key)
    {
        foreach (var kv in creds)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return kv.Value;
            }
        }
        return null;
    }
}
