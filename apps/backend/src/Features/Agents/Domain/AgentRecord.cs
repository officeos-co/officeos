namespace OffceOs.Domain.Features.Agents;

public sealed class AgentRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string? Model { get; set; }

    public AgentStatus Status { get; set; } = AgentStatus.Pending;

    public string? PodName { get; set; }

    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Optional system prompt the agent boots with. Set at create time and
    /// editable later via PatchAsync.
    /// </summary>
    public string? Prompt { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }

    /// <summary>FK → UserRecord.Id. Set at creation time from the authenticated user.</summary>
    public Guid? OwnerId { get; init; }

    /// <summary>
    /// Bearer token the agent pod presents back to this backend on
    /// <c>/api/agents/me/*</c>. Generated on create. Stored
    /// DataProtection-wrapped; plaintext is only handed to the pod at
    /// deploy time via the <c>ZEROCLAW_SKILLS_BACKEND_TOKEN</c> env var.
    /// </summary>
    public string? EncryptedBackendToken { get; set; }

    // ── Aggregate children (populated by rich-load repository methods) ───────

    public IReadOnlyList<AgentPersonalityRecord> PersonalityFiles { get; init; } = [];
    public IReadOnlyList<AgentMemoryRecord> Memories { get; init; } = [];
    public IReadOnlyList<AgentCronJobRecord> CronJobs { get; init; } = [];
    public IReadOnlyList<AgentRateLimitRecord> RateLimits { get; init; } = [];
    public IReadOnlyList<AgentChannelBindingRecord> ChannelBindings { get; init; } = [];
    public AgentSessionRecord? ActiveSession { get; init; }

    // ── Domain logic ─────────────────────────────────────────────────────────

    /// <summary>Whether this agent has a deployed pod.</summary>
    public bool HasPod => !string.IsNullOrEmpty(PodName);

    /// <summary>Marks the agent as successfully deployed.</summary>
    public void MarkDeployed(string podName, string serviceUrl)
    {
        PodName = podName;
        ServiceUrl = serviceUrl;
        Status = AgentStatus.Running;
    }

    /// <summary>Marks the agent as failed to deploy.</summary>
    public void MarkFailed()
    {
        Status = AgentStatus.Failed;
    }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static AgentRecord Create(string name, string provider, string? model, Guid? ownerId, string? prompt = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Agent name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required.", nameof(provider));

        var record = new AgentRecord
        {
            Name = name.Trim(),
            Provider = provider.Trim().ToLowerInvariant(),
            Status = AgentStatus.Pending,
            OwnerId = ownerId,
            Prompt = string.IsNullOrWhiteSpace(prompt) ? null : prompt,
        };
        record.ValidateAndSetModel(model);
        return record;
    }

    /// <summary>Validates and sets the model, defaulting to "auto" if null/empty.</summary>
    public void ValidateAndSetModel(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            if (ProviderRegistry.IsCustomProvider(Provider))
                throw new InvalidOperationException("Custom provider requires a configured concrete model.");

            Model = ProviderRegistry.DefaultModel;
            return;
        }

        var trimmed = model.Trim();
        if (ProviderRegistry.IsCustomProvider(Provider))
        {
            if (trimmed.Equals(ProviderRegistry.DefaultModel, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Custom provider does not support auto model routing.");

            Model = trimmed;
            return;
        }

        if (!ProviderRegistry.IsValidModel(trimmed))
        {
            var allowed = string.Join(", ", ProviderRegistry.SupportedModels);
            throw new InvalidOperationException(
                $"Model '{trimmed}' is not a known model. Allowed: {allowed}");
        }

        Model = trimmed;
    }
}
