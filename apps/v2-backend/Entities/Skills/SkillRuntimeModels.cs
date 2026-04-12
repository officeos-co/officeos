using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.Skills;

/// <summary>Result from POST /execute on the skill runtime.</summary>
public sealed class SkillExecutionResult
{
    public required bool Success { get; set; }
    public JsonElement? Result { get; set; }
    public string? Error { get; set; }
    public SessionMeta? SessionMeta { get; set; }
}

public sealed class SessionContext
{
    public string? SessionId { get; set; }
    public string? CookiesJson { get; set; }
}

public sealed class SessionMeta
{
    public string? SessionId { get; set; }
    public string? CookiesJson { get; set; }
}

/// <summary>Manifest returned by GET /manifests from the skill runtime.</summary>
public sealed class RuntimeManifest
{
    public required string Name { get; set; }
    public required string Title { get; set; }
    public required string Emoji { get; set; }
    public required string Description { get; set; }
    public required string Doc { get; set; }
    public required Dictionary<string, RuntimeActionManifest> Actions { get; set; }
    public required List<RuntimeCredentialField> CredentialFields { get; set; }
}

public sealed class RuntimeActionManifest
{
    public required string Description { get; set; }
    public JsonElement? Params { get; set; }
    public JsonElement? Returns { get; set; }
}

public sealed class RuntimeCredentialField
{
    public required string Key { get; set; }
    public required string Label { get; set; }
    public required string Kind { get; set; }
    public required bool Required { get; set; }
    public string? Placeholder { get; set; }
    public string? Help { get; set; }
}
