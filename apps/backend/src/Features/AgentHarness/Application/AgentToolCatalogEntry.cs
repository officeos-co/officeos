namespace OffceOs.Features.AgentHarness.Application;

public sealed record AgentToolCatalogEntry(
    string Group,
    string RuntimeName,
    string PermissionSkill,
    string PermissionTool,
    string Description,
    bool Deferred);
