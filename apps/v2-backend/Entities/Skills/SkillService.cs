using System.Text.Json;
using EnterpriseAgentOs.Api.Entities.AgentSkills;

namespace EnterpriseAgentOs.Api.Entities.Skills;

public sealed class SkillService : ISkillService
{
    private static readonly HashSet<string> SystemSkills = new(StringComparer.OrdinalIgnoreCase) { "browser" };

    private readonly ISkillCredentialRepository _repository;
    private readonly IAgentSkillRepository _agentSkills;
    private readonly SkillCredentialProtector _protector;
    private readonly SkillRuntimeClient _runtime;
    private readonly ILogger<SkillService> _logger;

    public SkillService(
        ISkillCredentialRepository repository,
        IAgentSkillRepository agentSkills,
        SkillCredentialProtector protector,
        SkillRuntimeClient runtime,
        ILogger<SkillService> logger)
    {
        _repository = repository;
        _agentSkills = agentSkills;
        _protector = protector;
        _runtime = runtime;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SkillDto>> ListAsync(CancellationToken ct = default)
    {
        var rows = (await _repository.ListAsync(ct))
            .ToDictionary(r => r.SkillName, StringComparer.OrdinalIgnoreCase);
        var runtimeManifests = await _runtime.GetManifestsAsync(ct);

        return runtimeManifests
            .Select(m =>
            {
                rows.TryGetValue(m.Name, out var row);
                return ToDto(m, row);
            })
            .ToList();
    }

    public async Task<SkillDto?> GetAsync(string name, CancellationToken ct = default)
    {
        var manifest = await GetRuntimeManifestAsync(name, ct);
        if (manifest is null) return null;
        var row = await _repository.GetByNameAsync(manifest.Name, ct);
        return ToDto(manifest, row);
    }

    public async Task<SkillDto?> InstallAsync(string name, CancellationToken ct = default)
    {
        var manifest = await GetRuntimeManifestAsync(name, ct);
        if (manifest is null)
        {
            _logger.LogWarning("Install failed: skill {SkillName} not found in runtime", name);
            return null;
        }
        var row = await _repository.UpsertAsync(manifest.Name, enabled: true, encryptedCredentials: null, ct);
        _logger.LogInformation("Skill {SkillName} installed", manifest.Name);
        return ToDto(manifest, row);
    }

    public async Task<SkillDto?> UninstallAsync(string name, CancellationToken ct = default)
    {
        var manifest = await GetRuntimeManifestAsync(name, ct);
        if (manifest is null)
        {
            _logger.LogWarning("Uninstall failed: skill {SkillName} not found in runtime", name);
            return null;
        }
        var row = await _repository.UpsertAsync(manifest.Name, enabled: false, encryptedCredentials: null, ct);
        _logger.LogInformation("Skill {SkillName} uninstalled", manifest.Name);
        return ToDto(manifest, row);
    }

    public async Task<SkillDto?> PutCredentialsAsync(
        string name,
        IReadOnlyDictionary<string, string> credentials,
        CancellationToken ct = default)
    {
        var manifest = await GetRuntimeManifestAsync(name, ct);
        if (manifest is null) return null;

        var credFields = ToCredentialFields(manifest);

        // Validate required fields.
        var missing = credFields
            .Where(f => f.Required && string.IsNullOrWhiteSpace(GetField(credentials, f.Key)))
            .Select(f => f.Key)
            .ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing required credential fields for skill '{manifest.Name}': {string.Join(", ", missing)}");
        }

        // Only keep fields the manifest knows about.
        var known = credFields.Select(f => f.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var filtered = credentials
            .Where(kv => known.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var json = JsonSerializer.Serialize(filtered);
        var ciphertext = _protector.Protect(json);

        var row = await _repository.UpsertAsync(manifest.Name, enabled: null, encryptedCredentials: ciphertext, ct);
        _logger.LogInformation("Credentials updated for skill {SkillName} ({FieldCount} fields)",
            manifest.Name, filtered.Count);
        return ToDto(manifest, row);
    }

    public async Task<IReadOnlyDictionary<string, string>?> GetDecryptedCredentialsAsync(
        string name,
        CancellationToken ct = default)
    {
        var manifest = await GetRuntimeManifestAsync(name, ct);
        if (manifest is null) return null;
        var row = await _repository.GetByNameAsync(manifest.Name, ct);
        if (row?.Enabled != true || string.IsNullOrEmpty(row.EncryptedCredentials))
        {
            if (SystemSkills.Contains(name))
                return new Dictionary<string, string>();
            return null;
        }
        var plaintext = _protector.Unprotect(row.EncryptedCredentials);
        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(plaintext);
        return parsed;
    }

    public async Task<CapabilitiesResponse> ListCapabilitiesAsync(Guid? agentId = null, CancellationToken ct = default)
    {
        var rows = (await _repository.ListAsync(ct))
            .ToDictionary(r => r.SkillName, StringComparer.OrdinalIgnoreCase);
        var caps = new List<CapabilityDto>();
        var docs = new List<SkillDocDto>();

        // If an agentId is provided, only include skills assigned to that agent (plus system skills).
        HashSet<string>? assignedSkills = null;
        if (agentId.HasValue)
        {
            var names = await _agentSkills.ListSkillNamesByAgentAsync(agentId.Value, ct);
            assignedSkills = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        }

        var runtimeManifests = await _runtime.GetManifestsAsync(ct);

        foreach (var manifest in runtimeManifests)
        {
            var isSystem = SystemSkills.Contains(manifest.Name);
            rows.TryGetValue(manifest.Name, out var row);
            if (!isSystem && (row is null || !row.Enabled || string.IsNullOrEmpty(row.EncryptedCredentials))) continue;

            // When filtering by agent, skip non-system skills that are not assigned.
            if (assignedSkills is not null && !isSystem && !assignedSkills.Contains(manifest.Name)) continue;

            foreach (var (actionName, action) in manifest.Actions)
            {
                var toolName = $"{manifest.Name}.{actionName}";
                var parameters = action.Params is JsonElement p
                    ? p
                    : JsonDocument.Parse("{}").RootElement;
                caps.Add(new CapabilityDto(
                    Skill: manifest.Name,
                    Name: toolName,
                    Description: action.Description,
                    Parameters: parameters,
                    Route: $"/api/agents/me/skills/{manifest.Name}/{actionName}"));
            }

            docs.Add(new SkillDocDto(manifest.Name, manifest.Doc));
        }
        return new CapabilitiesResponse(caps, docs);
    }

    public async Task<SkillDto?> SetRunTargetAsync(string name, string runTarget, CancellationToken ct = default)
    {
        var manifest = await GetRuntimeManifestAsync(name, ct);
        if (manifest is null)
        {
            _logger.LogWarning("SetRunTarget failed: skill {SkillName} not found in runtime", name);
            return null;
        }
        _logger.LogInformation("Setting run target for skill {SkillName} to {RunTarget}", name, runTarget);

        var n = name.Trim().ToLowerInvariant();
        var row = await _repository.GetByNameAsync(n, ct);
        if (row is null)
        {
            row = await _repository.UpsertAsync(n, enabled: false, encryptedCredentials: null, ct);
        }
        row.RunTarget = runTarget == "runner" ? "runner" : null;
        await _repository.SetRunTargetAsync(n, row.RunTarget, ct);
        row = await _repository.GetByNameAsync(n, ct);
        return ToDto(manifest, row);
    }

    public async Task<string> GetRunTargetAsync(string name, CancellationToken ct = default)
    {
        var row = await _repository.GetByNameAsync(name.Trim().ToLowerInvariant(), ct);
        return row?.RunTarget ?? "cloud";
    }

    private async Task<RuntimeManifest?> GetRuntimeManifestAsync(string name, CancellationToken ct)
    {
        var manifests = await _runtime.GetManifestsAsync(ct);
        return manifests.FirstOrDefault(m =>
            string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static SkillDto ToDto(RuntimeManifest manifest, SkillCredentialRecord? row)
    {
        var isSystem = SystemSkills.Contains(manifest.Name);
        var tools = manifest.Actions
            .Select(kv =>
            {
                var parameters = kv.Value.Params is JsonElement p
                    ? p
                    : JsonDocument.Parse("{}").RootElement;
                return new LlmToolDto($"{manifest.Name}.{kv.Key}", kv.Value.Description, parameters);
            })
            .ToList();

        return new SkillDto(
            Name: manifest.Name,
            Title: manifest.Title,
            Description: manifest.Description,
            Emoji: manifest.Emoji,
            Installed: isSystem || row?.Enabled == true,
            Configured: isSystem || !string.IsNullOrEmpty(row?.EncryptedCredentials),
            RunTarget: row?.RunTarget ?? "cloud",
            IsSystem: isSystem,
            CredentialFields: ToCredentialFields(manifest),
            LlmTools: tools);
    }

    private static IReadOnlyList<CredentialField> ToCredentialFields(RuntimeManifest manifest) =>
        manifest.CredentialFields
            .Select(f => new CredentialField(
                Key: f.Key,
                Label: f.Label,
                Kind: f.Kind,
                Required: f.Required,
                Placeholder: f.Placeholder,
                Help: f.Help))
            .ToList();

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
