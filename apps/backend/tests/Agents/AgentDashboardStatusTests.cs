using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Application.Features.Agents;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Agents;
using OffceOs.Infrastructure.Features.Agents;
using OffceOs.Infrastructure.Features.Channels;
using OffceOs.Infrastructure.Features.Context;
using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.Agents;

public sealed class AgentDashboardStatusTests
{
    [Theory]
    [InlineData("booting", "booting")]
    [InlineData("restarting", "restarting")]
    [InlineData("running", "idle")]
    [InlineData("failed", "failed")]
    public async Task Dashboard_status_reflects_runtime_state(string runtimeStatus, string expectedStatus)
    {
        await using var db = TestDbFactory.Create("agent-dashboard-status");
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        await AddAgentAsync(db, agentId, workspaceId, AgentStatus.Booting, "agent-pod");
        var deployer = new FakeAgentDeployer { Status = runtimeStatus };
        var service = CreateService(db, deployer);

        var result = await service.GetDashboardAgentAsync(agentId, Guid.NewGuid(), workspaceId);

        Assert.NotNull(result);
        Assert.Equal(expectedStatus, result.Status.ToStorageString());
    }

    [Fact]
    public async Task Dashboard_status_is_working_when_runtime_is_running_and_run_is_active()
    {
        await using var db = TestDbFactory.Create("agent-dashboard-working-status");
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        await AddAgentAsync(db, agentId, workspaceId, AgentStatus.Idle, "agent-pod");
        var runRepository = new AgentRunRepository(db);
        await runRepository.CreateAsync(new AgentRunRecord
        {
            AgentId = agentId,
            WorkspaceId = workspaceId,
            Status = "running",
            Name = "Agent turn",
            Prompt = "work",
        });
        var service = CreateService(db, new FakeAgentDeployer { Status = "running" });

        var result = await service.GetDashboardAgentAsync(agentId, Guid.NewGuid(), workspaceId);

        Assert.NotNull(result);
        Assert.Equal(AgentStatus.Working, result.Status);
    }

    [Fact]
    public async Task Dashboard_status_is_booting_when_agent_has_no_pod_and_has_not_failed()
    {
        await using var db = TestDbFactory.Create("agent-dashboard-no-pod-status");
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        await AddAgentAsync(db, agentId, workspaceId, AgentStatus.Booting, null);
        var service = CreateService(db, new FakeAgentDeployer());

        var result = await service.GetDashboardAgentAsync(agentId, Guid.NewGuid(), workspaceId);

        Assert.NotNull(result);
        Assert.Equal(AgentStatus.Booting, result.Status);
    }

    private static AgentDashboardService CreateService(EaosDbContext db, FakeAgentDeployer deployer)
    {
        var agentRepository = new AgentRepository(db);
        var channelRepository = new ChannelRepository(db);
        var agentDefinitionParser = new AgentDefinitionParser();
        return new AgentDashboardService(
            new MinimalAgentService(),
            agentRepository,
            new AgentSessionRepository(db),
            new AgentResourceRepository(db),
            new MemoryStoreRepository(db),
            channelRepository,
            new FakeChannelService(),
            new FakeBrowserService(),
            deployer,
            new AgentRunRepository(db),
            new FakeAgentLogService(),
            agentDefinitionParser);
    }

    private static async Task AddAgentAsync(
        EaosDbContext db,
        Guid agentId,
        Guid workspaceId,
        AgentStatus status,
        string? podName)
    {
        db.Agents.Add(new AgentEntity
        {
            Id = agentId,
            Name = "Agent",
            Provider = "openai",
            Model = "gpt-4o-mini",
            Status = status.ToStorageString(),
            PodName = podName,
            ServiceUrl = podName is null ? null : "http://agent-pod",
            CreatedAt = DateTime.UtcNow,
            WorkspaceId = workspaceId,
        });
        await db.SaveChangesAsync();
    }

    private sealed class MinimalAgentService : IAgentService
    {
        public Task<IReadOnlyList<AgentRecord>> ListAsync(AgentFilter filter, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentRecord>>([]);

        public Task<AgentRecord?> GetByAsync(AgentFilter filter, CancellationToken ct = default) =>
            Task.FromResult<AgentRecord?>(null);

        public Task<AgentRecord> CreateAsync(CreateAgentRequest request, Guid? ownerId = null, Guid? workspaceId = null, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<AgentRecord?> PatchAsync(Guid id, PatchAgentRequest request, CancellationToken ct = default) =>
            Task.FromResult<AgentRecord?>(null);

        public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(false);

        public Task InitializeAgentAsync(Guid agentId, Guid userId, AgentInitRequest init, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
