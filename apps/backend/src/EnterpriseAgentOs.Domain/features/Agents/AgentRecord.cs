namespace EnterpriseAgentOs.Domain.Features.Agents;

public sealed class AgentRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string? Model { get; set; }

    public string Status { get; set; } = "pending";

    public string? PodName { get; set; }

    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Optional system prompt the agent boots with. Set at create time
    /// (e.g. from a Quickstart template) and editable later via PatchAsync.
    /// </summary>
    public string? Prompt { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }

    /// <summary>FK → UserRecord.Id. Set at creation time from the authenticated user.</summary>
    public Guid? OwnerId { get; set; }

    /// <summary>
    /// Bearer token the agent pod presents back to this backend on
    /// <c>/api/agents/me/*</c>. Generated on create. Stored
    /// DataProtection-wrapped; plaintext is only handed to the pod at
    /// deploy time via the <c>ZEROCLAW_SKILLS_BACKEND_TOKEN</c> env var.
    /// </summary>
    public string? EncryptedBackendToken { get; set; }

    // ── Domain logic ─────────────────────────────────────────────────────────

    /// <summary>Whether this agent has a deployed pod.</summary>
    public bool HasPod => !string.IsNullOrEmpty(PodName);

    /// <summary>Marks the agent as successfully deployed.</summary>
    public void MarkDeployed(string podName, string serviceUrl)
    {
        PodName = podName;
        ServiceUrl = serviceUrl;
        Status = "running";
    }

    /// <summary>Marks the agent as failed to deploy.</summary>
    public void MarkFailed()
    {
        Status = "failed";
    }

    /// <summary>Validates and sets the model, defaulting to "auto" if null/empty.</summary>
    public void ValidateAndSetModel(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            Model = KnownModels.DefaultModel;
            return;
        }

        var trimmed = model.Trim();
        if (!KnownModels.IsValid(trimmed))
        {
            var allowed = string.Join(", ", KnownModels.SupportedModels);
            throw new InvalidOperationException(
                $"Model '{trimmed}' is not a known model. Allowed: {allowed}");
        }

        Model = trimmed;
    }
}
