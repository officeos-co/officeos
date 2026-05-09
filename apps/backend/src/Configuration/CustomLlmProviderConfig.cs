namespace EnterpriseAgentOs.Configuration;

public sealed class CustomLlmProviderConfig
{
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ModelId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string ModelDisplayName { get; init; } = string.Empty;
    public int CostWeight { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BaseUrl) &&
        !string.IsNullOrWhiteSpace(ModelId);

    public string EffectiveDisplayName =>
        string.IsNullOrWhiteSpace(DisplayName) ? "Self-hosted" : DisplayName.Trim();

    public string EffectiveModelDisplayName =>
        string.IsNullOrWhiteSpace(ModelDisplayName) ? ModelId.Trim() : ModelDisplayName.Trim();

    public int EffectiveCostWeight => CostWeight > 0 ? CostWeight : 20;

    public string? ApiKeyOrNull => string.IsNullOrWhiteSpace(ApiKey) ? null : ApiKey.Trim();
}
