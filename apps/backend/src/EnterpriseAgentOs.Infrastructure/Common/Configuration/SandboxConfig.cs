namespace EnterpriseAgentOs.Infrastructure.Common.Configuration;

public sealed class KubernetesConfig
{
    public string Namespace { get; init; } = string.Empty;
    public string Image { get; init; } = string.Empty;
}

public sealed class DockerConfig
{
    public string Image { get; init; } = string.Empty;
    public string Network { get; init; } = string.Empty;
    public string SocketPath { get; init; } = string.Empty;
}
