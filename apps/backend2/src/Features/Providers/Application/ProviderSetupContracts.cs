namespace OffceOs.Application.Features.Providers;

public interface IProviderSetupService
{
    Task<IReadOnlyList<ProviderSetupStatusResult>> GetSetupStatusAsync(Guid actorUserId, Guid organizationId, CancellationToken ct = default);
    Task<OrganizationProviderProfileRecord> SaveBedrockSetupAsync(Guid actorUserId, BedrockProviderSetupRequest request, CancellationToken ct = default);
    Task<OrganizationProviderProfileRecord> SaveVertexSetupAsync(Guid actorUserId, VertexProviderSetupRequest request, CancellationToken ct = default);
    Task<OrganizationProviderProfileRecord> SaveFoundrySetupAsync(Guid actorUserId, FoundryProviderSetupRequest request, CancellationToken ct = default);
    Task<ProviderModelAccessCheckResult> CheckModelAccessAsync(Guid actorUserId, Guid organizationId, string provider, string model, CancellationToken ct = default);
}

public sealed record BedrockProviderSetupRequest(
    Guid OrganizationId,
    string DisplayName,
    string? AwsRegion,
    ProviderAuthKind AuthKind,
    string? AwsProfile,
    string? AwsAccessKeyId,
    string? AwsSecretAccessKey,
    string? AwsSessionToken,
    string? BedrockApiKey,
    string? BaseUrl,
    bool SkipProviderAuth,
    IReadOnlyList<string> PinnedModels,
    bool Enabled);

public sealed record VertexProviderSetupRequest(
    Guid OrganizationId,
    string DisplayName,
    string? ProjectId,
    string? Location,
    ProviderAuthKind AuthKind,
    string? CredentialsPath,
    string? BaseUrl,
    bool SkipProviderAuth,
    IReadOnlyList<string> PinnedModels,
    bool Enabled);

public sealed record FoundryProviderSetupRequest(
    Guid OrganizationId,
    string DisplayName,
    string? Resource,
    string? BaseUrl,
    ProviderAuthKind AuthKind,
    string? ApiKey,
    bool SkipProviderAuth,
    IReadOnlyList<string> PinnedModels,
    bool Enabled);

public sealed record ProviderSetupStatusResult(
    string Provider,
    string DisplayName,
    bool Configured,
    bool Enabled,
    string AuthKind,
    DateTime ConfiguredAt,
    IReadOnlyList<string> PinnedModels,
    IReadOnlyDictionary<string, string> Environment);

public sealed record ProviderModelAccessCheckResult(
    string Provider,
    string Model,
    bool Accessible,
    string Message);
