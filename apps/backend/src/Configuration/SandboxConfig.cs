namespace EnterpriseAgentOs.Configuration;

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
    public string Host { get; init; } = "127.0.0.1";
    public bool PublishHostPort { get; init; } = true;
}

public sealed class WorkspaceStorageConfig
{
    public string Endpoint { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string Bucket { get; init; } = string.Empty;
}
