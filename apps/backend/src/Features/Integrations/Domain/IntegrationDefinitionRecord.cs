namespace OffceOs.Features.Integrations.Domain;

public sealed record IntegrationDefinitionRecord
{
    private readonly Guid _id = Guid.NewGuid();

    public Guid Id
    {
        get => IsBuiltin ? DeterministicGuid(Name) : _id;
        init => _id = value;
    }

    public string Name { get; init; } = string.Empty;
    public Guid? OwnerId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IntegrationTransportType TransportType { get; init; }
    public string? Command { get; init; }
    public string? Args { get; init; }
    public string? Url { get; init; }
    public string? Logo { get; init; }
    public string? Category { get; init; }
    public string? CredentialFieldsJson { get; init; }
    public string? OauthProvider { get; init; }
    public string? OauthScopesJson { get; init; }
    public bool OauthConfigured { get; init; }
    public string Subtitle { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string AuthorUrl { get; init; } = string.Empty;
    public string DocumentationUrl { get; init; } = string.Empty;
    public string RepositoryUrl { get; init; } = string.Empty;
    public IReadOnlyList<IntegrationCatalogToolRecord> Tools { get; init; } = [];
    public string? CapabilitiesJson { get; init; }
    public IReadOnlyList<string> Entities { get; init; } = [];
    public bool IsBuiltin { get; init; }
    public bool CredentialConfigured { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    private static Guid DeterministicGuid(string name)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"integration-definition:{name}"));
        return new Guid(hash.AsSpan(0, 16));
    }
}

public sealed record IntegrationCatalogToolRecord(
    string Name,
    string Description,
    object? Parameters);
