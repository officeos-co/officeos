namespace EnterpriseAgentOs.Infrastructure.Common.Configuration;

public sealed class KubernetesConfig
{
    public string Namespace { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
}
