namespace OffceOs.Domain.Features.ControlPlane;

public abstract class ControlPlaneResourceDefinition
{
    public abstract string Kind { get; }
    public abstract string Singular { get; }
    public virtual IReadOnlyList<string> Aliases => [Singular];
    public abstract string DisplayName { get; }
    public abstract string Description { get; }
    public abstract string Icon { get; }
    public abstract IReadOnlyList<string> DisplayFields { get; }
    protected virtual bool SupportsDelete => true;

    public virtual IReadOnlyList<ControlPlaneResourceCapabilityRecord> Capabilities =>
        SupportsDelete
            ?
            [
                ControlPlaneResourceCapabilityRegistry.List,
                ControlPlaneResourceCapabilityRegistry.Describe,
                ControlPlaneResourceCapabilityRegistry.Delete,
                ControlPlaneResourceCapabilityRegistry.Logs,
            ]
            :
            [
                ControlPlaneResourceCapabilityRegistry.List,
                ControlPlaneResourceCapabilityRegistry.Describe,
                ControlPlaneResourceCapabilityRegistry.Logs,
            ];

    public ControlPlaneResourceDescriptor ToDescriptor() => new(
        Kind,
        Singular,
        Aliases,
        DisplayName,
        Description,
        Icon,
        Capabilities,
        DisplayFields);
}

public sealed class AgentControlPlaneResourceDefinition : ControlPlaneResourceDefinition
{
    public override string Kind => "agents";
    public override string Singular => "agent";
    public override string DisplayName => "Agents";
    public override string Description => "Agent resources";
    public override string Icon => "hubot";
    public override IReadOnlyList<string> DisplayFields => ["name", "status", "provider", "model"];
}

public sealed class BrowserControlPlaneResourceDefinition : ControlPlaneResourceDefinition
{
    public override string Kind => "browsers";
    public override string Singular => "browser";
    public override string DisplayName => "Browsers";
    public override string Description => "Browser resources";
    public override string Icon => "browser";
    public override IReadOnlyList<string> DisplayFields => ["name", "displayName", "currentAgentId"];
}

public sealed class ChannelControlPlaneResourceDefinition : ControlPlaneResourceDefinition
{
    public override string Kind => "channels";
    public override string Singular => "channel";
    public override string DisplayName => "Channels";
    public override string Description => "Channel connections";
    public override string Icon => "broadcast";
    public override IReadOnlyList<string> DisplayFields => ["name", "platform", "enabled"];
}

public sealed class ControlPlaneControlPlaneResourceDefinition : ControlPlaneResourceDefinition
{
    public override string Kind => "control-plane";
    public override string Singular => "control-plane";
    public override IReadOnlyList<string> Aliases => ["controlplane", "system"];
    public override string DisplayName => "Control Plane";
    public override string Description => "Workspace control plane logs";
    public override string Icon => "server-process";
    public override IReadOnlyList<string> DisplayFields => ["name", "workspaceId"];
    protected override bool SupportsDelete => false;
}

public sealed class CredentialControlPlaneResourceDefinition : ControlPlaneResourceDefinition
{
    public override string Kind => "credentials";
    public override string Singular => "credential";
    public override string DisplayName => "Credentials";
    public override string Description => "Routine credentials";
    public override string Icon => "key";
    public override IReadOnlyList<string> DisplayFields => ["name", "provider", "authKind", "configured"];
}

public sealed class IntegrationControlPlaneResourceDefinition : ControlPlaneResourceDefinition
{
    public override string Kind => "integrations";
    public override string Singular => "integration";
    public override string DisplayName => "Integrations";
    public override string Description => "Integration deployments";
    public override string Icon => "plug";
    public override IReadOnlyList<string> DisplayFields => ["name", "server", "status"];
}

public sealed class MemoryStoreControlPlaneResourceDefinition : ControlPlaneResourceDefinition
{
    public override string Kind => "memory-stores";
    public override string Singular => "memory-store";
    public override IReadOnlyList<string> Aliases => ["memory-store", "memorystore", "memorystores"];
    public override string DisplayName => "Memory Stores";
    public override string Description => "Memory stores";
    public override string Icon => "database";
    public override IReadOnlyList<string> DisplayFields => ["name", "entryCount", "updatedAt"];
}

public sealed class ModelControlPlaneResourceDefinition : ControlPlaneResourceDefinition
{
    public override string Kind => "models";
    public override string Singular => "model";
    public override string DisplayName => "Models";
    public override string Description => "Provider models";
    public override string Icon => "symbol-method";
    public override IReadOnlyList<string> DisplayFields => ["id", "displayName", "provider"];
    protected override bool SupportsDelete => false;
}

public sealed class ProviderControlPlaneResourceDefinition : ControlPlaneResourceDefinition
{
    public override string Kind => "providers";
    public override string Singular => "provider";
    public override string DisplayName => "Providers";
    public override string Description => "Configured provider resources";
    public override string Icon => "server-process";
    public override IReadOnlyList<string> DisplayFields => ["name", "type", "configured", "phase"];
}

public sealed class RoutineControlPlaneResourceDefinition : ControlPlaneResourceDefinition
{
    public override string Kind => "routines";
    public override string Singular => "routine";
    public override string DisplayName => "Routines";
    public override string Description => "Agent routines";
    public override string Icon => "clock";
    public override IReadOnlyList<string> DisplayFields => ["name", "agentName", "enabled", "createdAt"];
}
