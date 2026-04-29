namespace EnterpriseAgentOs.Infrastructure.Common.Configuration;

public sealed class DockerConfig
{
    public bool Enabled { get; set; }
    public string Image { get; set; } = "harkro123/eaos-pod-executor:latest";
    public string Network { get; set; } = "enterpriseagentos_default";
    public string SocketPath { get; set; } = "/var/run/docker.sock";
}
