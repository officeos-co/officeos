namespace EnterpriseAgentOs.Application.Features.Agents.Integrations;

internal sealed class IntegrationExecuteTool : IAgentTool
{
    private readonly IIntegrationExecutionService _execution;

    public IntegrationExecuteTool(IIntegrationExecutionService execution)
    {
        _execution = execution;
    }

    public string Name => "integration_execute";

    public ToolSchema Schema => new(
        Name,
        "Execute an integration operation. Use context_store_search for indexed search and list/get for direct provider requests.",
        Parameters);

    public AgentToolKind Kind => AgentToolKind.Network;
    public bool AlwaysLoad => true;
    public bool IsReadOnly => true;

    internal static object Parameters => new
    {
        type = "object",
        additionalProperties = false,
        required = new[] { "source_id", "entity", "action", "params" },
        properties = new
        {
            source_id = new
            {
                type = "string",
                format = "uuid",
                description = "Integration connection/source id.",
            },
            entity = new
            {
                type = "string",
                description = "Integration entity, for example repositories, issues, pull_requests, or commits.",
            },
            action = new
            {
                type = "string",
                @enum = new[] { "get", "list", "api_search", "context_store_search", "create", "update" },
                description = "Connector action.",
            },
            @params = new
            {
                type = "object",
                additionalProperties = true,
                description = "Action parameters.",
            },
            select_fields = new
            {
                type = "array",
                items = new { type = "string" },
                description = "Optional field projection.",
            },
        },
    };

    public Task<ToolValidationResult> ValidateAsync(JsonElement args, CancellationToken ct = default)
    {
        if (!args.TryGetProperty("source_id", out var sourceId) || !Guid.TryParse(sourceId.GetString(), out _))
            return Task.FromResult(ToolValidationResult.Invalid("source_id must be a valid integration connection UUID."));
        if (!args.TryGetProperty("entity", out var entity) || string.IsNullOrWhiteSpace(entity.GetString()))
            return Task.FromResult(ToolValidationResult.Invalid("entity is required."));
        if (!args.TryGetProperty("action", out var action) || string.IsNullOrWhiteSpace(action.GetString()))
            return Task.FromResult(ToolValidationResult.Invalid("action is required."));
        if (!args.TryGetProperty("params", out var parameters) || parameters.ValueKind != JsonValueKind.Object)
            return Task.FromResult(ToolValidationResult.Invalid("params must be an object."));
        return Task.FromResult(ToolValidationResult.Valid);
    }

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var request = new IntegrationExecuteRequest(
            Guid.Parse(args.GetProperty("source_id").GetString()!),
            args.GetProperty("entity").GetString()!,
            args.GetProperty("action").GetString()!,
            args.GetProperty("params"),
            ReadSelectFields(args));
        var response = await _execution.ExecuteAsync(request, ct);
        return new ToolResult(true, JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static IReadOnlyList<string>? ReadSelectFields(JsonElement args)
        => args.TryGetProperty("select_fields", out var fields) && fields.ValueKind == JsonValueKind.Array
            ? fields.EnumerateArray().Select(f => f.GetString()).Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f!).ToList()
            : null;
}
