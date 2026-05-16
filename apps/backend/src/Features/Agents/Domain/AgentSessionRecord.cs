using OffceOs.Domain.Common.ValueObjects;

namespace OffceOs.Domain.Features.Agents;

/// <summary>
/// Represents an agent conversation session (OpenClaw-style).
/// Sessions isolate conversation context — starting a new session clears history
/// and optionally runs the bootstrap ritual.
/// </summary>
public sealed class AgentSessionRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; init; }

    public SessionStatus Status { get; set; } = SessionStatus.Active;

    public int MessageCount { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    public AgentRecord? Agent { get; init; }

    // ── Computed properties ────────────────────────────────────────────

    public bool IsActive => Status == SessionStatus.Active;

    // ── Factory ────────────────────────────────────────────────────────

    public static AgentSessionRecord Create(Guid agentId) => new()
    {
        AgentId = agentId,
    };

    // ── Domain methods ─────────────────────────────────────────────────

    /// <summary>Ends this session. Idempotent — second call is a no-op.</summary>
    public void End()
    {
        if (!IsActive) return;
        Status = SessionStatus.Ended;
        EndedAt = DateTime.UtcNow;
    }

    /// <summary>Records activity to keep the session alive.</summary>
    public void RecordActivity(DateTime at)
    {
        LastActivityAt = at;
    }

    /// <summary>Increments the message counter for this session.</summary>
    public void IncrementMessageCount()
    {
        MessageCount++;
        RecordActivity(DateTime.UtcNow);
    }

    /// <summary>
    /// Formats the bootstrap message for this session.
    /// First session: uses BOOTSTRAP.md content if present.
    /// Subsequent sessions: generic "New session started" message.
    /// </summary>
    public string FormatBootstrapMessage(
        IReadOnlyList<AgentPersonalityRecord> personalityFiles,
        bool isFirstSession = true)
    {
        if (isFirstSession)
        {
            var bootstrapFile = personalityFiles.FirstOrDefault(f =>
                string.Equals(f.FileName, "BOOTSTRAP.md", StringComparison.OrdinalIgnoreCase));

            if (bootstrapFile is not null)
                return bootstrapFile.Content;
        }

        return "New session started. Previous conversation context has been cleared. " +
               "Session started at " + CreatedAt.ToString("O") + " UTC.";
    }
}
