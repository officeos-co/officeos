namespace OffceOs.Application.Features.Providers;

internal sealed class ProviderSetupService : IProviderSetupService
{
    private readonly IOrganizationProviderProfileRepository _organizationProviderProfileRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationProviderProfileService _organizationProviderProfileService;
    private readonly CredentialProtector _credentialProtector;
    private readonly ProviderEnterprisePolicy _providerEnterprisePolicy;
    private readonly LlmProviderDispatcher _llmProviderDispatcher;

    public ProviderSetupService(
        IOrganizationProviderProfileRepository organizationProviderProfileRepository,
        IOrganizationRepository organizationRepository,
        IOrganizationProviderProfileService organizationProviderProfileService,
        CredentialProtector credentialProtector,
        ProviderEnterprisePolicy providerEnterprisePolicy,
        LlmProviderDispatcher llmProviderDispatcher)
    {
        _organizationProviderProfileRepository = organizationProviderProfileRepository;
        _organizationRepository = organizationRepository;
        _organizationProviderProfileService = organizationProviderProfileService;
        _credentialProtector = credentialProtector;
        _providerEnterprisePolicy = providerEnterprisePolicy;
        _llmProviderDispatcher = llmProviderDispatcher;
    }

    public async Task<IReadOnlyList<ProviderSetupStatusResult>> GetSetupStatusAsync(Guid actorUserId, Guid organizationId, CancellationToken ct = default)
    {
        await RequireOrganizationAdminAsync(actorUserId, organizationId, ct);
        await _providerEnterprisePolicy.RequireEnterpriseOrganizationAsync(organizationId, ct);

        var profiles = await _organizationProviderProfileRepository.ListAsync(
            new OrganizationProviderProfileFilter { OrganizationId = organizationId },
            ct);

        return profiles
            .Where(profile => ProviderRegistry.IsEnterpriseProvider(profile.Provider))
            .Select(ToStatus)
            .ToList();
    }

    public async Task<OrganizationProviderProfileRecord> SaveBedrockSetupAsync(Guid actorUserId, BedrockProviderSetupRequest request, CancellationToken ct = default)
    {
        var credentials = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var authKind = request.SkipProviderAuth ? ProviderAuthKind.Gateway : request.AuthKind;

        if (!string.IsNullOrWhiteSpace(request.BaseUrl))
            credentials["baseUrl"] = request.BaseUrl.Trim();

        switch (authKind)
        {
            case ProviderAuthKind.Gateway:
                Require(credentials, "baseUrl");
                credentials["skipAuth"] = "true";
                AddOptional(credentials, "awsRegion", request.AwsRegion);
                break;
            case ProviderAuthKind.AwsEnvironment:
                credentials["awsRegion"] = RequireValue(request.AwsRegion, "awsRegion");
                break;
            case ProviderAuthKind.AwsProfile:
                credentials["awsRegion"] = RequireValue(request.AwsRegion, "awsRegion");
                credentials["awsProfile"] = RequireValue(request.AwsProfile, "awsProfile");
                break;
            case ProviderAuthKind.AwsAccessKey:
            case ProviderAuthKind.AwsIam:
                authKind = ProviderAuthKind.AwsAccessKey;
                credentials["awsRegion"] = RequireValue(request.AwsRegion, "awsRegion");
                credentials["awsAccessKeyId"] = RequireValue(request.AwsAccessKeyId, "awsAccessKeyId");
                credentials["awsSecretAccessKey"] = RequireValue(request.AwsSecretAccessKey, "awsSecretAccessKey");
                AddOptional(credentials, "awsSessionToken", request.AwsSessionToken);
                break;
            case ProviderAuthKind.AwsBedrockApiKey:
                credentials["awsRegion"] = RequireValue(request.AwsRegion, "awsRegion");
                credentials["apiKey"] = RequireValue(request.BedrockApiKey, "apiKey");
                break;
            default:
                throw new InvalidOperationException($"Authentication kind '{request.AuthKind.ToStorageString()}' is not supported for Bedrock setup.");
        }

        return await _organizationProviderProfileService.SaveNativeAuthAsync(
            actorUserId,
            request.OrganizationId,
            ProviderRegistry.AwsBedrockProviderSlug,
            request.DisplayName,
            request.PinnedModels,
            authKind,
            credentials,
            request.Enabled,
            ct);
    }

    public async Task<OrganizationProviderProfileRecord> SaveVertexSetupAsync(Guid actorUserId, VertexProviderSetupRequest request, CancellationToken ct = default)
    {
        var credentials = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var authKind = request.SkipProviderAuth ? ProviderAuthKind.Gateway : request.AuthKind;

        AddOptional(credentials, "baseUrl", request.BaseUrl);
        switch (authKind)
        {
            case ProviderAuthKind.Gateway:
                Require(credentials, "baseUrl");
                credentials["skipAuth"] = "true";
                AddOptional(credentials, "projectId", request.ProjectId);
                AddOptional(credentials, "location", request.Location);
                break;
            case ProviderAuthKind.GoogleApplicationDefault:
                credentials["projectId"] = RequireValue(request.ProjectId, "projectId");
                credentials["location"] = RequireValue(request.Location, "location");
                break;
            case ProviderAuthKind.GoogleServiceAccountFile:
                credentials["projectId"] = RequireValue(request.ProjectId, "projectId");
                credentials["location"] = RequireValue(request.Location, "location");
                credentials["credentialsPath"] = RequireValue(request.CredentialsPath, "credentialsPath");
                break;
            default:
                throw new InvalidOperationException($"Authentication kind '{request.AuthKind.ToStorageString()}' is not supported for Vertex setup.");
        }

        return await _organizationProviderProfileService.SaveNativeAuthAsync(
            actorUserId,
            request.OrganizationId,
            ProviderRegistry.GoogleVertexProviderSlug,
            request.DisplayName,
            request.PinnedModels,
            authKind,
            credentials,
            request.Enabled,
            ct);
    }

    public async Task<OrganizationProviderProfileRecord> SaveFoundrySetupAsync(Guid actorUserId, FoundryProviderSetupRequest request, CancellationToken ct = default)
    {
        var credentials = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var authKind = request.SkipProviderAuth ? ProviderAuthKind.Gateway : request.AuthKind;

        AddOptional(credentials, "baseUrl", request.BaseUrl);
        AddOptional(credentials, "resource", request.Resource);
        switch (authKind)
        {
            case ProviderAuthKind.Gateway:
                Require(credentials, "baseUrl");
                credentials["skipAuth"] = "true";
                break;
            case ProviderAuthKind.AzureDefaultCredential:
                RequireFoundryEndpoint(credentials);
                break;
            case ProviderAuthKind.AzureApiKey:
                RequireFoundryEndpoint(credentials);
                credentials["apiKey"] = RequireValue(request.ApiKey, "apiKey");
                break;
            default:
                throw new InvalidOperationException($"Authentication kind '{request.AuthKind.ToStorageString()}' is not supported for Foundry setup.");
        }

        return await _organizationProviderProfileService.SaveNativeAuthAsync(
            actorUserId,
            request.OrganizationId,
            ProviderRegistry.AzureFoundryProviderSlug,
            request.DisplayName,
            request.PinnedModels,
            authKind,
            credentials,
            request.Enabled,
            ct);
    }

    public async Task<ProviderModelAccessCheckResult> CheckModelAccessAsync(
        Guid actorUserId,
        Guid organizationId,
        string provider,
        string model,
        CancellationToken ct = default)
    {
        await RequireOrganizationAdminAsync(actorUserId, organizationId, ct);
        await _providerEnterprisePolicy.RequireEnterpriseOrganizationAsync(organizationId, ct);

        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var profile = await _organizationProviderProfileRepository.GetByAsync(
            new OrganizationProviderProfileFilter
            {
                OrganizationId = organizationId,
                Provider = normalizedProvider,
                Enabled = true,
            },
            ct);
        if (profile is null)
            throw new InvalidOperationException($"Provider '{normalizedProvider}' is not configured.");

        var pinnedModels = ParseModels(profile.AllowedModelsJson);
        if (!pinnedModels.Contains(model, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Model '{model}' is not pinned for provider '{normalizedProvider}'.");

        var auth = ToAuth(_credentialProtector.Unprotect(profile.EncryptedApiKey));
        var result = await _llmProviderDispatcher.CheckModelAccessAsync(normalizedProvider, auth, model, ct);
        return result.IsSuccess
            ? new ProviderModelAccessCheckResult(normalizedProvider, model, true, "Model is accessible.")
            : new ProviderModelAccessCheckResult(normalizedProvider, model, false, result.Error.Message);
    }

    private async Task RequireOrganizationAdminAsync(Guid userId, Guid organizationId, CancellationToken ct)
    {
        var members = await _organizationRepository.ListMembersAsync(organizationId, ct);
        var member = members.FirstOrDefault(m => m.UserId == userId && m.Status == MemberStatus.Active);
        if (member?.Role is not (OrgRole.Owner or OrgRole.Admin))
            throw new InvalidOperationException("Organization not found.");
    }

    private ProviderSetupStatusResult ToStatus(OrganizationProviderProfileRecord profile)
    {
        var credentials = _credentialProtector.Unprotect(profile.EncryptedApiKey);
        var auth = ToAuth(credentials);
        var pinnedModels = ParseModels(profile.AllowedModelsJson);
        var environment = BuildEnvironment(profile.Provider, auth, pinnedModels);
        return new ProviderSetupStatusResult(
            profile.Provider,
            profile.DisplayName,
            profile.Enabled && !string.IsNullOrWhiteSpace(profile.EncryptedApiKey),
            profile.Enabled,
            auth.Kind.ToStorageString(),
            profile.ConfiguredAt,
            pinnedModels,
            environment);
    }

    private static Dictionary<string, string> BuildEnvironment(string provider, ProviderAuthResult auth, IReadOnlyList<string> pinnedModels)
    {
        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        switch (provider)
        {
            case ProviderRegistry.AwsBedrockProviderSlug:
                env["CLAUDE_CODE_USE_BEDROCK"] = "1";
                AddOptional(env, "AWS_REGION", auth.Get("awsRegion"));
                AddOptional(env, "AWS_PROFILE", auth.Get("awsProfile"));
                AddOptional(env, "ANTHROPIC_BEDROCK_BASE_URL", auth.Get("baseUrl"));
                if (auth.Kind == ProviderAuthKind.Gateway)
                    env["CLAUDE_CODE_SKIP_BEDROCK_AUTH"] = "1";
                if (auth.Kind == ProviderAuthKind.AwsBedrockApiKey)
                    env["AWS_BEARER_TOKEN_BEDROCK"] = "<configured>";
                if (auth.Kind == ProviderAuthKind.AwsAccessKey || auth.Kind == ProviderAuthKind.AwsIam)
                {
                    env["AWS_ACCESS_KEY_ID"] = "<configured>";
                    env["AWS_SECRET_ACCESS_KEY"] = "<configured>";
                    if (!string.IsNullOrWhiteSpace(auth.Get("awsSessionToken")))
                        env["AWS_SESSION_TOKEN"] = "<configured>";
                }

                break;
            case ProviderRegistry.GoogleVertexProviderSlug:
                env["CLAUDE_CODE_USE_VERTEX"] = "1";
                AddOptional(env, "CLOUD_ML_REGION", auth.Get("location"));
                AddOptional(env, "ANTHROPIC_VERTEX_PROJECT_ID", auth.Get("projectId"));
                AddOptional(env, "GOOGLE_APPLICATION_CREDENTIALS", auth.Get("credentialsPath"));
                AddOptional(env, "ANTHROPIC_VERTEX_BASE_URL", auth.Get("baseUrl"));
                if (auth.Kind == ProviderAuthKind.Gateway)
                    env["CLAUDE_CODE_SKIP_VERTEX_AUTH"] = "1";
                break;
            case ProviderRegistry.AzureFoundryProviderSlug:
                env["CLAUDE_CODE_USE_FOUNDRY"] = "1";
                AddOptional(env, "ANTHROPIC_FOUNDRY_RESOURCE", auth.Get("resource"));
                AddOptional(env, "ANTHROPIC_FOUNDRY_BASE_URL", auth.Get("baseUrl"));
                if (auth.Kind == ProviderAuthKind.Gateway)
                    env["CLAUDE_CODE_SKIP_FOUNDRY_AUTH"] = "1";
                if (auth.Kind == ProviderAuthKind.AzureApiKey)
                    env["ANTHROPIC_FOUNDRY_API_KEY"] = "<configured>";
                break;
            case ProviderRegistry.OpenAiCodexProviderSlug:
                AddOptional(env, "CODEX_ACCOUNT_EMAIL", auth.Get("accountEmail"));
                AddOptional(env, "CODEX_PLAN_TYPE", auth.Get("planType"));
                break;
        }

        AddModelPins(env, pinnedModels);
        return env;
    }

    private static void AddModelPins(Dictionary<string, string> env, IReadOnlyList<string> pinnedModels)
    {
        foreach (var model in pinnedModels)
        {
            if (model.Contains("opus", StringComparison.OrdinalIgnoreCase))
                env["ANTHROPIC_DEFAULT_OPUS_MODEL"] = model;
            else if (model.Contains("sonnet", StringComparison.OrdinalIgnoreCase))
                env["ANTHROPIC_DEFAULT_SONNET_MODEL"] = model;
            else if (model.Contains("haiku", StringComparison.OrdinalIgnoreCase))
                env["ANTHROPIC_DEFAULT_HAIKU_MODEL"] = model;
        }
    }

    private static ProviderAuthResult ToAuth(IReadOnlyDictionary<string, string> credentials)
    {
        var kind = credentials.TryGetValue("authKind", out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToProviderAuthKind()
            : ProviderAuthKind.ApiKey;
        return new ProviderAuthResult(kind, new Dictionary<string, string>(credentials, StringComparer.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> ParseModels(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);
            return parsed.ValueKind == JsonValueKind.Array
                ? parsed.EnumerateArray().Select(item => item.GetString()).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!).ToList()
                : [];
        }
        catch
        {
            return [];
        }
    }

    private static void RequireFoundryEndpoint(IReadOnlyDictionary<string, string> credentials)
    {
        if (!credentials.ContainsKey("resource") && !credentials.ContainsKey("baseUrl"))
            throw new InvalidOperationException("Provider credential 'resource' or 'baseUrl' is required.");
    }

    private static string RequireValue(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Provider credential '{key}' is required.");
        return value.Trim();
    }

    private static void Require(IReadOnlyDictionary<string, string> credentials, string key)
    {
        if (!credentials.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Provider credential '{key}' is required.");
    }

    private static void AddOptional(IDictionary<string, string> credentials, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            credentials[key] = value.Trim();
    }
}
