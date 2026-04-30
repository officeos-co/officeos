namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class SkillService : ISkillService
{
    private static readonly HashSet<string> SystemSkills = new(StringComparer.OrdinalIgnoreCase) { "browser" };

    private readonly ISkillRepository _skillRepository;
    private readonly ISkillCatalogRepository _skillCatalogRepository;
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly SkillCredentialProtector _skillCredentialProtector;
    private readonly SkillRuntimeClient _skillRuntimeClient;
    private readonly ILogger<SkillService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly EaosDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly GoogleOAuthConfig _googleOAuthConfig;

    private static readonly TimeSpan SkillCacheTtl = TimeSpan.FromMinutes(5);
    private const string SkillListCacheKey = "skills:list";
    private static string SkillCacheKey(string name) => $"skills:{name}";

    public SkillService(
        ISkillRepository repository,
        ISkillCatalogRepository catalog,
        IAgentSkillRepository agentSkills,
        SkillCredentialProtector protector,
        SkillRuntimeClient runtime,
        ILogger<SkillService> logger,
        IMemoryCache cache,
        EaosDbContext db,
        IHttpClientFactory httpFactory,
        GoogleOAuthConfig googleOAuthConfig)
    {
        _skillRepository = repository;
        _skillCatalogRepository = catalog;
        _agentSkillRepository = agentSkills;
        _skillCredentialProtector = protector;
        _skillRuntimeClient = runtime;
        _logger = logger;
        _memoryCache = cache;
        _db = db;
        _httpFactory = httpFactory;
        _googleOAuthConfig = googleOAuthConfig;
    }

    public async Task<IReadOnlyList<SkillDto>> ListAsync(CancellationToken ct = default)
    {
        if (_memoryCache.TryGetValue(SkillListCacheKey, out IReadOnlyList<SkillDto>? cached) && cached is not null)
            return cached;

        var skills = await _skillCatalogRepository.ListActiveAsync(ct);
        var rows = (await _skillRepository.ListAsync(ct))
            .ToDictionary(r => r.SkillName, StringComparer.OrdinalIgnoreCase);

        var result = skills
            .Select(s =>
            {
                rows.TryGetValue(s.Name, out var row);
                return ToDto(s, row);
            })
            .ToList();

        _memoryCache.Set(SkillListCacheKey, (IReadOnlyList<SkillDto>)result,
            new MemoryCacheEntryOptions
            { AbsoluteExpirationRelativeToNow = SkillCacheTtl });
        return result;
    }

    public async Task<SkillDto?> GetAsync(string name, CancellationToken ct = default)
    {
        var key = SkillCacheKey(name);
        if (_memoryCache.TryGetValue(key, out SkillDto? cached) && cached is not null)
            return cached;

        var skill = await _skillCatalogRepository.GetByNameAsync(name, ct);
        if (skill is null || !skill.IsActive) return null;
        var row = await _skillRepository.GetByNameAsync(skill.Name, ct);
        var dto = ToDto(skill, row);

        _memoryCache.Set(key, dto,
            new MemoryCacheEntryOptions
            { AbsoluteExpirationRelativeToNow = SkillCacheTtl });
        return dto;
    }

    public async Task<SkillDto?> InstallAsync(string name, CancellationToken ct = default)
    {
        var n = name.Trim().ToLowerInvariant();

        var manifests = await _skillRuntimeClient.GetManifestsAsync(ct);
        var liveManifest = manifests.FirstOrDefault(m =>
            string.Equals(m.Name.Trim(), n, StringComparison.OrdinalIgnoreCase));

        if (liveManifest is not null)
        {
            var systemSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "browser" };
            var record = SkillRecord.CreateBuiltin(n, liveManifest, systemSkills.Contains(n));
            await _skillCatalogRepository.UpsertAsync(record, ct);
        }

        var skill = await _skillCatalogRepository.GetByNameAsync(n, ct);
        if (skill is null)
        {
            _logger.LogWarning("Install failed: skill {SkillName} not found in catalog", n);
            return null;
        }
        var row = await _skillRepository.UpsertAsync(skill.Name, enabled: true, encryptedCredentials: null, ct);
        _logger.LogInformation("Skill {SkillName} installed", skill.Name);
        _memoryCache.Remove(SkillListCacheKey);
        _memoryCache.Remove(SkillCacheKey(skill.Name));
        return ToDto(skill, row);
    }

    public async Task<SkillDto?> UninstallAsync(string name, CancellationToken ct = default)
    {
        var skill = await _skillCatalogRepository.GetByNameAsync(name, ct);
        if (skill is null)
        {
            _logger.LogWarning("Uninstall failed: skill {SkillName} not found in catalog", name);
            return null;
        }
        var row = await _skillRepository.UpsertAsync(skill.Name, enabled: false, encryptedCredentials: null, ct);
        _logger.LogInformation("Skill {SkillName} uninstalled", skill.Name);
        _memoryCache.Remove(SkillListCacheKey);
        _memoryCache.Remove(SkillCacheKey(skill.Name));
        return ToDto(skill, row);
    }

    public async Task<SkillDto?> PutCredentialsAsync(
        string name,
        IReadOnlyDictionary<string, string> credentials,
        CancellationToken ct = default)
    {
        var skill = await _skillCatalogRepository.GetByNameAsync(name, ct);
        if (skill is null) return null;

        var credFields = ToCredentialFields(skill.GetCredentialFields());

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
        var ciphertext = _skillCredentialProtector.Protect(json);

        var row = await _skillRepository.UpsertAsync(skill.Name, enabled: null, encryptedCredentials: ciphertext, ct);
        _logger.LogInformation("Credentials updated for skill {SkillName} ({FieldCount} fields)",
            skill.Name, filtered.Count);
        _memoryCache.Remove(SkillListCacheKey);
        _memoryCache.Remove(SkillCacheKey(skill.Name));
        return ToDto(skill, row);
    }

    public async Task<IReadOnlyDictionary<string, string>?> GetDecryptedCredentialsAsync(
        string name,
        CancellationToken ct = default)
    {
        var skill = await _skillCatalogRepository.GetByNameAsync(name, ct);
        if (skill is null) return null;
        var row = await _skillRepository.GetByNameAsync(skill.Name, ct);

        // Check if this skill uses OAuth2 credentials
        var credFields = skill.GetCredentialFields();
        var oauthField = credFields.FirstOrDefault(f => f.Kind == "oauth2" && f.Oauth2 is not null);
        if (oauthField is not null)
        {
            var token = await GetOAuthAccessTokenAsync(oauthField.Oauth2!.Provider, ct);
            if (token is null)
            {
                if (skill.IsSystem || SystemSkills.Contains(name))
                    return new Dictionary<string, string>();
                return null;
            }
            return new Dictionary<string, string> { [oauthField.Key] = token };
        }

        if (row?.Enabled != true || string.IsNullOrEmpty(row.EncryptedCredentials))
        {
            if (skill.IsSystem || SystemSkills.Contains(name))
                return new Dictionary<string, string>();
            return null;
        }
        var plaintext = _skillCredentialProtector.Unprotect(row.EncryptedCredentials);
        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(plaintext);
        return parsed;
    }

    private async Task<string?> GetOAuthAccessTokenAsync(string provider, CancellationToken ct)
    {
        var tokenRecord = await _db.OAuthTokens.FirstOrDefaultAsync(t => t.Provider == provider, ct);
        if (tokenRecord is null || string.IsNullOrEmpty(tokenRecord.EncryptedAccessToken))
            return null;

        // If token has no expiry (e.g. GitHub), skip refresh
        if (!tokenRecord.ExpiresAtUtc.HasValue)
            return _skillCredentialProtector.Unprotect(tokenRecord.EncryptedAccessToken);

        // If token is expired (or within 5 min of expiry), refresh it
        if (tokenRecord.ExpiresAtUtc.Value < DateTime.UtcNow.AddMinutes(5))
        {
            if (string.IsNullOrEmpty(tokenRecord.EncryptedRefreshToken))
            {
                _logger.LogWarning("OAuth token for {Provider} expired and no refresh token available", provider);
                return null;
            }

            var refreshToken = _skillCredentialProtector.Unprotect(tokenRecord.EncryptedRefreshToken);
            var newToken = await RefreshTokenAsync(provider, refreshToken, ct);
            if (newToken is null) return null;

            tokenRecord.EncryptedAccessToken = _skillCredentialProtector.Protect(newToken.AccessToken!);
            tokenRecord.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(newToken.ExpiresIn > 0 ? newToken.ExpiresIn : 3600);
            tokenRecord.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return newToken.AccessToken;
        }

        return _skillCredentialProtector.Unprotect(tokenRecord.EncryptedAccessToken);
    }

    private async Task<OAuthTokenRefreshResult?> RefreshTokenAsync(string provider, string refreshToken, CancellationToken ct)
    {
        var (tokenUrl, clientId, clientSecret) = provider switch
        {
            "google" => ("https://oauth2.googleapis.com/token", _googleOAuthConfig.ClientId, _googleOAuthConfig.ClientSecret),
            _ => throw new InvalidOperationException($"Token refresh not supported for provider: {provider}"),
        };

        var http = _httpFactory.CreateClient();
        var res = await http.PostAsync(tokenUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["refresh_token"] = refreshToken,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["grant_type"] = "refresh_token",
            }), ct);

        if (!res.IsSuccessStatusCode)
        {
            _logger.LogError("{Provider} token refresh failed: {Status}", provider, res.StatusCode);
            return null;
        }

        return await res.Content.ReadFromJsonAsync<OAuthTokenRefreshResult>(ct);
    }

    private sealed record OAuthTokenRefreshResult(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string? AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn);

    public async Task<CapabilitiesResponse> ListCapabilitiesAsync(Guid? agentId = null, CancellationToken ct = default)
    {
        var skills = await _skillCatalogRepository.ListActiveAsync(ct);
        var rows = (await _skillRepository.ListAsync(ct))
            .ToDictionary(r => r.SkillName, StringComparer.OrdinalIgnoreCase);
        var caps = new List<CapabilityDto>();
        var docs = new List<SkillDocDto>();

        HashSet<string>? assignedSkills = null;
        if (agentId.HasValue)
        {
            var names = await _agentSkillRepository.ListSkillNamesByAgentAsync(agentId.Value, ct);
            assignedSkills = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        }

        foreach (var skill in skills)
        {
            var isSystem = skill.IsSystem || SystemSkills.Contains(skill.Name);
            rows.TryGetValue(skill.Name, out var row);
            if (!isSystem && (row is null || !row.Enabled || string.IsNullOrEmpty(row.EncryptedCredentials))) continue;
            if (assignedSkills is not null && !isSystem && !assignedSkills.Contains(skill.Name)) continue;

            var actions = skill.GetActions();

            foreach (var (actionName, action) in actions)
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

            docs.Add(new SkillDocDto(skill.Name, skill.Doc ?? string.Empty));
        }
        return new CapabilitiesResponse(caps, docs);
    }

    public async Task<SkillDto?> SetRunTargetAsync(string name, string runTarget, CancellationToken ct = default)
    {
        var skill = await _skillCatalogRepository.GetByNameAsync(name, ct);
        if (skill is null)
        {
            _logger.LogWarning("SetRunTarget failed: skill {SkillName} not found in catalog", name);
            return null;
        }
        _logger.LogInformation("Setting run target for skill {SkillName} to {RunTarget}", name, runTarget);

        var n = name.Trim().ToLowerInvariant();
        var row = await _skillRepository.GetByNameAsync(n, ct);
        if (row is null)
        {
            row = await _skillRepository.UpsertAsync(n, enabled: false, encryptedCredentials: null, ct);
        }
        row.RunTarget = runTarget == "runner" ? RunTarget.Runner : null;
        await _skillRepository.SetRunTargetAsync(n, row.RunTarget?.ToStorageString(), ct);
        row = await _skillRepository.GetByNameAsync(n, ct);
        return ToDto(skill, row);
    }

    public async Task<string> GetRunTargetAsync(string name, CancellationToken ct = default)
    {
        var row = await _skillRepository.GetByNameAsync(name.Trim().ToLowerInvariant(), ct);
        return (row?.RunTarget ?? RunTarget.Cloud).ToStorageString();
    }

    private static SkillDto ToDto(SkillRecord skill, SkillCredentialRecord? row)
    {
        var actions = skill.GetActions();
        var credFields = skill.GetCredentialFields();
        var isSystem = skill.IsSystem || SystemSkills.Contains(skill.Name);
        var tools = actions
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
            Installed: isSystem || row?.Enabled == true,
            Configured: isSystem || !string.IsNullOrEmpty(row?.EncryptedCredentials),
            RunTarget: (row?.RunTarget ?? RunTarget.Cloud).ToStorageString(),
            IsSystem: isSystem,
            CredentialFields: ToCredentialFields(credFields),
            LlmTools: tools);
    }

    private static IReadOnlyList<CredentialField> ToCredentialFields(List<RuntimeCredentialField> credentialFields) =>
        credentialFields
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
