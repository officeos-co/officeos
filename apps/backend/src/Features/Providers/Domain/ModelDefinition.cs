namespace OffceOs.Features.Providers.Domain;

public sealed record ModelDefinition(
    string Id,
    string DisplayName,
    int CostWeight,
    SmartRoutingTier? SmartTier);

public enum SmartRoutingTier { Simple, Standard, Complex }
