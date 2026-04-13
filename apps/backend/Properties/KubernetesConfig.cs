namespace EnterpriseAgentOs.Api.Properties;

public sealed class KubernetesConfig
{
    public bool Enabled { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
}
