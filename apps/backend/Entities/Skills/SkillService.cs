using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.Skills;

public sealed class SkillService : ISkillService
{
    private static readonly HashSet<string> SystemSkills = new(StringComparer.OrdinalIgnoreCase) { "browser" };

    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISkillRepository _repository;
    private readonly ISkillCatalogRepository _catalog;
    private readonly IAgentSkillRepository _agentSkills;
    private readonly SkillCredentialProtector _protector;
    private readonly SkillRuntimeClient _runtime;
    private readonly ILogger<SkillService> _logger;

    public SkillService(
        ISkillRepository repository,
        ISkillCatalogRepository catalog,
        IAgentSkillRepository agentSkills,
        SkillCredentialProtector protector,
        SkillRuntimeClient runtime,
        ILogger<SkillService> logger)
    {
        _repository = repository;
        _catalog = catalog;
        _agentSkills = agentSkills;
        _protector = protector;
        _runtime = runtime;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SkillDto>> ListAsync(CancellationToken ct = default)
    {
        var skills = await _catalog.ListActiveAsync(ct);
        var rows = (await _repository.ListAsync(ct))
            .ToDictionary(r => r.SkillName, StringComparer.OrdinalIgnoreCase);

        return skills
            .Select(s =>
            {
                rows.TryGetValue(s.Name, out var row);
                return ToDto(s, row);
            })
            .ToList();
    }

    public async Task<SkillDto?> GetAsync(string name, CancellationToken ct = default)
    {
        var skill = await _catalog.GetByNameAsync(name, ct);
        if (skill is null || skill.Status != "active") return null;
        var row = await _repository.GetByNameAsync(skill.Name, ct);
        return ToDto(skill, row);
    }

    public async Task<SkillDto?> InstallAsync(string name, CancellationToken ct = default)
    {
        var skill = await _catalog.GetByNameAsync(name, ct);
        if (skill is null)
        {
            _logger.LogWarning("Install failed: skill {SkillName} not found in catalog", name);
            return null;
        }
        var row = await _repository.UpsertAsync(skill.Name, enabled: true, encryptedCredentials: null, ct);
        _logger.LogInformation("Skill {SkillName} installed", skill.Name);
        return ToDto(skill, row);
    }

    public async Task<SkillDto?> UninstallAsync(string name, CancellationToken ct = default)
    {
        var skill = await _catalog.GetByNameAsync(name, ct);
        if (skill is null)
        {
            _logger.LogWarning("Uninstall failed: skill {SkillName} not found in catalog", name);
            return null;
        }
        var row = await _repository.UpsertAsync(skill.Name, enabled: false, encryptedCredentials: null, ct);
        _logger.LogInformation("Skill {SkillName} uninstalled", skill.Name);
        return ToDto(skill, row);
    }

    public async Task<SkillDto?> PutCredentialsAsync(
        string name,
        IReadOnlyDictionary<string, string> credentials,
        CancellationToken ct = default)
    {
        var skill = await _catalog.GetByNameAsync(name, ct);
        if (skill is null) return null;

        var manifest = DeserializeManifest(skill);
        var credFields = ToCredentialFields(manifest);

        var missing = credFields
            .Where(f => f.Required && string.IsNullOrWhiteSpace(GetField(credentials, f.Key)))
            .Select(f => f.Key)
            .ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing required credential fields for skill '{skill.Name}': {string.Join(", ", missing)}");
        }

        var known = credFields.Select(f => f.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var filtered = credentials
            .Where(kv => known.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var json = JsonSerializer.Serialize(filtered);
        var ciphertext = _protector.Protect(json);

        var row = await _repository.UpsertAsync(skill.Name, enabled: null, encryptedCredentials: ciphertext, ct);
        _logger.LogInformation("Credentials updated for skill {SkillName} ({FieldCount} fields)",
            skill.Name, filtered.Count);
        return ToDto(skill, row);
    }

    public async Task<IReadOnlyDictionary<string, string>?> GetDecryptedCredentialsAsync(
        string name,
        CancellationToken ct = default)
    {
        var skill = await _catalog.GetByNameAsync(name, ct);
        if (skill is null) return null;
        var row = await _repository.GetByNameAsync(skill.Name, ct);
        if (row?.Enabled != true || string.IsNullOrEmpty(row.EncryptedCredentials))
        {
            if (skill.IsSystem || SystemSkills.Contains(name))
                return new Dictionary<string, string>();
            return null;
        }
        var plaintext = _protector.Unprotect(row.EncryptedCredentials);
        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(plaintext);
        return parsed;
    }

    public async Task<CapabilitiesResponse> ListCapabilitiesAsync(Guid? agentId = null, CancellationToken ct = default)
    {
        var skills = await _catalog.ListActiveAsync(ct);
        var rows = (await _repository.ListAsync(ct))
            .ToDictionary(r => r.SkillName, StringComparer.OrdinalIgnoreCase);
        var caps = new List<CapabilityDto>();
        var docs = new List<SkillDocDto>();

        HashSet<string>? assignedSkills = null;
        if (agentId.HasValue)
        {
            var names = await _agentSkills.ListSkillNamesByAgentAsync(agentId.Value, ct);
            assignedSkills = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        }

        foreach (var skill in skills)
        {
            var isSystem = skill.IsSystem || SystemSkills.Contains(skill.Name);
            rows.TryGetValue(skill.Name, out var row);
            if (!isSystem && (row is null || !row.Enabled || string.IsNullOrEmpty(row.EncryptedCredentials))) continue;
            if (assignedSkills is not null && !isSystem && !assignedSkills.Contains(skill.Name)) continue;

            var manifest = DeserializeManifest(skill);

            foreach (var (actionName, action) in manifest.Actions)
            {
                var toolName = $"{skill.Name}.{actionName}";
                var parameters = action.Params is JsonElement p
                    ? p
                    : JsonDocument.Parse("{}").RootElement;
                caps.Add(new CapabilityDto(
                    Skill: skill.Name,
                    Name: toolName,
                    Description: action.Description,
                    Parameters: parameters,
                    Route: $"/api/agents/me/skills/{skill.Name}/{actionName}"));
            }

            docs.Add(new SkillDocDto(skill.Name, manifest.Doc));
        }
        return new CapabilitiesResponse(caps, docs);
    }

    public async Task<SkillDto?> SetRunTargetAsync(string name, string runTarget, CancellationToken ct = default)
    {
        var skill = await _catalog.GetByNameAsync(name, ct);
        if (skill is null)
        {
            _logger.LogWarning("SetRunTarget failed: skill {SkillName} not found in catalog", name);
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
        return ToDto(skill, row);
    }

    public async Task<string> GetRunTargetAsync(string name, CancellationToken ct = default)
    {
        var row = await _repository.GetByNameAsync(name.Trim().ToLowerInvariant(), ct);
        return row?.RunTarget ?? "cloud";
    }

    private static RuntimeManifest DeserializeManifest(SkillRecord skill)
    {
        return JsonSerializer.Deserialize<RuntimeManifest>(skill.ManifestJson, ManifestJsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize manifest for skill '{skill.Name}'");
    }

    private static SkillDto ToDto(SkillRecord skill, SkillCredentialRecord? row)
    {
        var manifest = DeserializeManifest(skill);
        var isSystem = skill.IsSystem || SystemSkills.Contains(skill.Name);
        var tools = manifest.Actions
            .Select(kv =>
            {
                var parameters = kv.Value.Params is JsonElement p
                    ? p
                    : JsonDocument.Parse("{}").RootElement;
                return new LlmToolDto($"{skill.Name}.{kv.Key}", kv.Value.Description, parameters);
            })
            .ToList();

        return new SkillDto(
            Name: skill.Name,
            Title: skill.Title,
            Description: skill.Description,
            Emoji: skill.Emoji,
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
