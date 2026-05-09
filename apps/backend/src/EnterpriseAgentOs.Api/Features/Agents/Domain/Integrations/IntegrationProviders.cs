namespace EnterpriseAgentOs.Domain.Features.Agents.Integrations;

public abstract class IntegrationProvider
{
    public abstract IntegrationDefinitionRecord Definition { get; }

    public virtual IReadOnlyList<IntegrationCapabilityRecord> Capabilities => [];

    public virtual Task ValidateConnectionAsync(IntegrationConnectionRecord connection, CancellationToken ct = default)
        => Task.CompletedTask;
}

public abstract class ToolIntegrationProvider : IntegrationProvider
{
    public virtual IReadOnlyList<IntegrationToolDefinitionRecord> Tools => [];
}

public abstract class IndexableIntegrationProvider : ToolIntegrationProvider
{
    public abstract IReadOnlyList<string> SupportedEntities { get; }

    public abstract IReadOnlyList<string> NormalizeEntities(IReadOnlyList<string> entities);

    public abstract Task<IReadOnlyList<IntegrationIndexedRecordRecord>> FetchIndexRecordsAsync(
        IntegrationConnectionRecord connection,
        string entity,
        CancellationToken ct = default);
}

public sealed record IntegrationCapabilityRecord(
    string Type,
    string Name,
    string Description);

public sealed record IntegrationToolDefinitionRecord(
    string Name,
    string Description,
    bool IsReadOnly);

public static class IntegrationIndexAccess
{
    public const string ToolName = "__indexed_data";
}
