namespace OffceOs.Api.Features.Providers;

public sealed record OrganizationProviderProfilePayload(
    Guid Id,
    Guid OrganizationId,
    string Provider,
    string DisplayName,
    IReadOnlyList<string> AllowedModels,
    bool Enabled,
    DateTime ConfiguredAt);

public sealed record SaveOrganizationProviderProfileInput(
    Guid OrganizationId,
    string Provider,
    string DisplayName,
    IReadOnlyList<string> AllowedModels,
    string ApiKey,
    bool Enabled,
    string? AuthKind,
    IReadOnlyList<ProviderCredentialInput>? Credentials);

public sealed record ProviderCredentialInput(
    string Key,
    string Value);

public sealed record BedrockProviderSetupInput(
    Guid OrganizationId,
    string DisplayName,
    string? AwsRegion,
    string AuthKind,
    string? AwsProfile,
    string? AwsAccessKeyId,
    string? AwsSecretAccessKey,
    string? AwsSessionToken,
    string? BedrockApiKey,
    string? BaseUrl,
    bool SkipProviderAuth,
    IReadOnlyList<string> PinnedModels,
    bool Enabled);

public sealed record VertexProviderSetupInput(
    Guid OrganizationId,
    string DisplayName,
    string? ProjectId,
    string? Location,
    string AuthKind,
    string? CredentialsPath,
    string? BaseUrl,
    bool SkipProviderAuth,
    IReadOnlyList<string> PinnedModels,
    bool Enabled);

public sealed record FoundryProviderSetupInput(
    Guid OrganizationId,
    string DisplayName,
    string? Resource,
    string? BaseUrl,
    string AuthKind,
    string? ApiKey,
    bool SkipProviderAuth,
    IReadOnlyList<string> PinnedModels,
    bool Enabled);

public sealed record ProviderModelAccessCheckInput(
    Guid OrganizationId,
    string Provider,
    string Model);

public sealed record PollCodexOAuthLoginInput(
    string LoginId);

public sealed record CodexOAuthLoginPayload(
    string LoginId,
    string AuthUrl,
    DateTime ExpiresAt);

public sealed record CodexOAuthStatusPayload(
    string LoginId,
    bool Completed,
    bool Success,
    string? Error,
    string? AccountEmail,
    string? PlanType);

public sealed record ProviderSetupStatusPayload(
    string Provider,
    string DisplayName,
    bool Configured,
    bool Enabled,
    string AuthKind,
    DateTime ConfiguredAt,
    IReadOnlyList<string> PinnedModels,
    IReadOnlyList<ProviderEnvironmentPayload> Environment);

public sealed record ProviderEnvironmentPayload(
    string Key,
    string Value);

public sealed record ProviderModelAccessCheckPayload(
    string Provider,
    string Model,
    bool Accessible,
    string Message);

internal static class OrganizationProviderProfileGraphQLMapper
{
    public static OrganizationProviderProfilePayload ToPayload(OrganizationProviderProfileRecord record) => new(
        record.Id,
        record.OrganizationId,
        record.Provider,
        record.DisplayName,
        ParseList(record.AllowedModelsJson),
        record.Enabled,
        record.ConfiguredAt);

    public static ProviderSetupStatusPayload ToPayload(ProviderSetupStatusResult result) => new(
        result.Provider,
        result.DisplayName,
        result.Configured,
        result.Enabled,
        result.AuthKind,
        result.ConfiguredAt,
        result.PinnedModels,
        result.Environment
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new ProviderEnvironmentPayload(pair.Key, pair.Value))
            .ToList());

    public static ProviderModelAccessCheckPayload ToPayload(ProviderModelAccessCheckResult result) => new(
        result.Provider,
        result.Model,
        result.Accessible,
        result.Message);

    public static CodexOAuthLoginPayload ToPayload(CodexOAuthLoginResult result) => new(
        result.LoginId,
        result.AuthUrl,
        result.ExpiresAt);

    public static CodexOAuthStatusPayload ToPayload(CodexOAuthStatusResult result) => new(
        result.LoginId,
        result.Completed,
        result.Success,
        result.Error,
        result.AccountEmail,
        result.PlanType);

    private static IReadOnlyList<string> ParseList(string? json)
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
}
