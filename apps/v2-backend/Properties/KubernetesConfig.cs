namespace EnterpriseAgentOs.Api.Properties;

public sealed class KubernetesConfig
{
    public bool Enabled { get; init; }
    public string Namespace { get; init; } = string.Empty;
    public string Image { get; init; } = string.Empty;
}
