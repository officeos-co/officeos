using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.Skills;

public sealed record SkillDto(
    string Name,
    string Title,
    string Description,
    string Emoji,
    bool Installed,
    bool Configured,
    IReadOnlyList<CredentialField> CredentialFields,
    IReadOnlyList<LlmToolDto> LlmTools);

public sealed record LlmToolDto(
    string Name,
    string Description,
    JsonElement Parameters);

public sealed record CapabilityDto(
    string Skill,
    string Name,
    string Description,
    JsonElement Parameters,
    string Route);

public sealed record SkillDocDto(string Name, string Doc);

public sealed record CapabilitiesResponse(
    IReadOnlyList<CapabilityDto> Tools,
    IReadOnlyList<SkillDocDto> Skills);

public sealed record PutCredentialsRequest(Dictionary<string, string> Credentials);
