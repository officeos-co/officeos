namespace EnterpriseAgentOs.Domain.Common.Services;

public sealed record ModelDefinition(
    string Id,
    string DisplayName,
    int CostWeight,
    SmartRoutingTier? SmartTier);

public enum SmartRoutingTier { Simple, Standard, Complex }
