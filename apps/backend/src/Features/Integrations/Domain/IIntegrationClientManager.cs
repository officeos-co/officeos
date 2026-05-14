namespace OffceOs.Domain.Features.Integrations;

/// <summary>
/// Manages connections to integrations. Returns raw integration tool metadata.
/// The Application layer wraps these as runtime integration clients.
/// </summary>
public interface IIntegrationClientManager
{
    Task<IntegrationConnectionResult> ConnectAsync(
        IntegrationDefinitionRecord server,
        Dictionary<string, string> credentials,
        CancellationToken ct = default);
}

public sealed class IntegrationConnectionResult : IAsyncDisposable
{
    public IReadOnlyList<IntegrationDiscoveredTool> Tools { get; init; } = [];
    public object? NativeClient { get; init; }
    public IAsyncDisposable? Connection { get; init; }

    public async ValueTask DisposeAsync()
    {
        if (Connection is not null)
            await Connection.DisposeAsync();
    }
}

/// <summary>
/// A tool discovered from an integration. Holds an opaque reference
/// for engine adapters or integration clients that need native execution.
/// </summary>
public sealed class IntegrationDiscoveredTool
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public object? Parameters { get; init; }
    public string IntegrationName { get; init; } = string.Empty;
    /// <summary>Opaque reference to the SDK tool + client for execution.</summary>
    public object NativeHandle { get; init; } = null!;
}
