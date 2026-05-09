namespace EnterpriseAgentOs.Domain.Features.Integrations;

/// <summary>
/// Manages connections to integrations. Returns raw integration tool metadata.
/// The Application layer wraps these as IAgentTool instances.
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
/// so the Application layer can create an IAgentTool wrapper.
/// </summary>
public sealed class IntegrationDiscoveredTool
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? JsonSchema { get; init; }
    public string IntegrationName { get; init; } = string.Empty;
    /// <summary>Opaque reference to the SDK tool + client for execution.</summary>
    public object NativeHandle { get; init; } = null!;
}
