namespace EnterpriseAgentOs.Infrastructure.Common.Configuration;

public sealed class WhatsAppGatewayConfig
{
    /// <summary>
    /// Base URL of the WhatsApp Baileys sidecar.
    /// Default: http://localhost:3001 (runs as sidecar in the same pod).
    /// </summary>
    public string Url { get; set; } = "http://localhost:3002";
}
