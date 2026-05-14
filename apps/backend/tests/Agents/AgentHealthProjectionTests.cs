using OffceOs.Application.Features.Agents;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Agents;
using Xunit;

namespace OffceOs.Tests.Agents;

public sealed class AgentHealthProjectionTests
{
    [Fact]
    public void From_marks_agent_healthy_after_active_bootstrap_succeeds()
    {
        var definitionId = Guid.NewGuid();
        var agent = Agent(definitionId);
        var run = Bootstrap(agent, definitionId, "completed");

        var health = AgentHealthProjection.From(agent, [run]);

        Assert.Equal("Healthy", health.Status);
        Assert.Equal("green", health.State);
        Assert.Equal("BootstrapSucceeded", health.Reason);
    }

    [Fact]
    public void From_marks_agent_failed_when_latest_active_bootstrap_fails()
    {
        var definitionId = Guid.NewGuid();
        var agent = Agent(definitionId);
        var run = Bootstrap(agent, definitionId, "failed", "OpenCode exited with 1.");

        var health = AgentHealthProjection.From(agent, [run]);

        Assert.Equal("Failed", health.Status);
        Assert.Equal("red", health.State);
        Assert.Equal("BootstrapFailed", health.Reason);
        Assert.Contains("OpenCode exited with 1", health.Message);
    }

    [Fact]
    public void From_marks_agent_pending_when_definition_has_not_bootstrapped()
    {
        var previousDefinitionId = Guid.NewGuid();
        var activeDefinitionId = Guid.NewGuid();
        var agent = Agent(activeDefinitionId);
        var previousRun = Bootstrap(agent, previousDefinitionId, "completed");

        var health = AgentHealthProjection.From(agent, [previousRun]);

        Assert.Equal("Pending", health.Status);
        Assert.Equal("orange", health.State);
        Assert.Equal("DefinitionChangedNeedsBootstrap", health.Reason);
    }

    private static AgentRecord Agent(Guid definitionId) => new()
    {
        Id = Guid.NewGuid(),
        Name = "engineering-agent",
        Provider = "openai",
        Model = "gpt-4o-mini",
        Status = AgentStatus.Idle,
        ActiveDefinitionId = definitionId,
    };

    private static AgentRunRecord Bootstrap(
        AgentRecord agent,
        Guid definitionId,
        string status,
        string? error = null) => new()
    {
        AgentId = agent.Id,
        DefinitionId = definitionId,
        Purpose = AgentRunPurposeKinds.Bootstrap,
        Kind = "opencode",
        Status = status,
        Prompt = "Bootstrap",
        Error = error,
        CreatedAt = DateTime.UtcNow,
        CompletedAt = status == "completed" || status == "failed" ? DateTime.UtcNow : null,
    };
}
