namespace OffceOs.Domain.Features.ControlPlane;

public static class ControlPlaneResourceRegistry
{
    public static IReadOnlyList<ControlPlaneResourceDescriptor> Resources { get; } =
    [
        new("agents", "agent", ["agent"], "Agents", "Agent resources", "hubot", ["list", "describe", "delete", "logs"], ["name", "status", "provider", "model"]),
        new("browsers", "browser", ["browser"], "Browsers", "Browser resources", "browser", ["list", "describe", "delete"], ["name", "displayName", "currentAgentId"]),
        new("channels", "channel", ["channel"], "Channels", "Channel connections", "broadcast", ["list", "describe", "delete"], ["name", "platform", "enabled"]),
        new("credentials", "credential", ["credential"], "Credentials", "Routine credentials", "key", ["list", "describe", "delete"], ["name", "provider", "authKind", "configured"]),
        new("integrations", "integration", ["integration"], "Integrations", "Integration deployments", "plug", ["list", "describe", "delete"], ["name", "server", "status"]),
        new("memory-stores", "memory-store", ["memory-store", "memorystore", "memorystores"], "Memory Stores", "Memory stores", "database", ["list", "describe", "delete"], ["name", "entryCount", "updatedAt"]),
        new("models", "model", ["model"], "Models", "Provider models", "symbol-method", ["list", "describe"], ["id", "displayName", "provider"]),
        new("providers", "provider", ["provider"], "Providers", "Configured provider resources", "server-process", ["list", "describe", "delete"], ["name", "type", "configured", "phase"]),
        new("routines", "routine", ["routine"], "Routines", "Agent routines", "clock", ["list", "describe", "delete"], ["name", "agentName", "enabled", "createdAt"]),
    ];
}
