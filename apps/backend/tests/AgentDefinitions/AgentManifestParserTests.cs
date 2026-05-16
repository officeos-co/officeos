using OffceOs.Application.Features.AgentDefinitions;
using OffceOs.Domain.Features.Channels;
using OffceOs.Domain.Features.Providers;

namespace OffceOs.Tests.AgentDefinitions;

public sealed class AgentManifestParserTests
{
    [Fact]
    public void Parse_reads_multi_resource_declarative_manifest()
    {
        var parser = new DeclarativeManifestParser();

        var manifest = parser.Parse(
            """
            apiVersion: officeos.io/v1
            kind: Channel
            metadata:
              name: support-slack
            spec:
              type: slack
              token: xoxb-test
            ---
            apiVersion: officeos.io/v1
            kind: Agent
            metadata:
              name: support-agent
            spec:
              provider: anthropic
              model: claude-sonnet-4-6
              description: Answers customer questions.
              system: Answer from sources.
              channels:
                - ref: support-slack
            """);

        Assert.Equal(2, manifest.Items.Count);
        Assert.Equal("Channel", manifest.Items[0].Kind);
        Assert.Equal("support-slack", manifest.Items[0].Metadata?.Name);
        Assert.Equal("Agent", manifest.Items[1].Kind);
        Assert.Equal("support-agent", manifest.Items[1].Metadata?.Name);
    }

    [Fact]
    public void Serialize_writes_multi_document_yaml()
    {
        var parser = new DeclarativeManifestParser();
        var manifest = parser.Parse(
            """
            apiVersion: officeos.io/v1
            kind: MemoryStore
            metadata:
              name: product-docs
            spec:
              displayName: Product Docs
            """);

        var yaml = parser.Serialize(manifest);

        Assert.Contains("kind: MemoryStore", yaml);
        Assert.Contains("name: product-docs", yaml);
        Assert.Contains("displayName: Product Docs", yaml);
    }

    [Fact]
    public void Parse_preserves_yaml_scalar_types()
    {
        var parser = new DeclarativeManifestParser();

        var manifest = parser.Parse(
            """
            apiVersion: officeos.io/v1
            kind: Channel
            metadata:
              name: support-slack
            spec:
              type: slack
              enabled: true
              retryCount: 3
            """);

        var spec = manifest.Items[0].Spec!.Value;
        Assert.True(spec.GetProperty("enabled").GetBoolean());
        Assert.Equal(3, spec.GetProperty("retryCount").GetInt32());
    }

    [Fact]
    public async Task Validate_rejects_outdated_api_versions()
    {
        var service = new DeclarativeAgentService(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new AgentDefinitionParser(),
            new DeclarativeManifestParser(),
            null!);

        var result = await service.ValidateAsync(
            """
            apiVersion: eaos.dev/v1
            kind: Channel
            metadata:
              name: support-slack
            spec:
              type: slack
              token: xoxb-test
            """,
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.False(result.Valid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("apiVersion must be officeos.io/v1.", error.Message);
    }

    [Fact]
    public async Task Validate_allows_references_that_already_exist_in_workspace()
    {
        var workspaceId = Guid.NewGuid();
        var service = new DeclarativeAgentService(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new ExistingChannelRepository(),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new AgentDefinitionParser(),
            new DeclarativeManifestParser(),
            null!,
            new ExistingProviderResourceRepository());

        var result = await service.ValidateAsync(
            """
            apiVersion: officeos.io/v1
            kind: Agent
            metadata:
              name: support-agent
            spec:
              provider: anthropic
              model: claude-sonnet-4-6
              channels:
                - ref: support-slack
            """,
            Guid.NewGuid(),
            workspaceId);

        Assert.True(result.Valid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_accepts_declared_provider_resource_for_agent()
    {
        var service = CreateValidationService();

        var result = await service.ValidateAsync(
            """
            apiVersion: officeos.io/v1
            kind: Provider
            metadata:
              name: openai
            spec:
              type: openai
              models:
                - gpt-4o-mini
              credentials:
                apiKey: sk-test
            ---
            apiVersion: officeos.io/v1
            kind: Agent
            metadata:
              name: support-agent
            spec:
              provider: openai
              model: gpt-4o-mini
            """,
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.True(result.Valid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_accepts_channel_permission_policy()
    {
        var service = CreateValidationService();

        var result = await service.ValidateAsync(
            """
            apiVersion: officeos.io/v1
            kind: Channel
            metadata:
              name: internal-support
            spec:
              type: internal
              permissionPolicy:
                type: allow_list
                tools:
                  - internal_channel_send
            """,
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.True(result.Valid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_accepts_dispatch_only_provider_with_pinned_models()
    {
        var service = CreateValidationService();

        var result = await service.ValidateAsync(
            """
            apiVersion: officeos.io/v1
            kind: Provider
            metadata:
              name: groq
            spec:
              type: groq
              models:
                - llama-3.3-70b-versatile
              credentials:
                apiKey: gsk-test
            """,
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.True(result.Valid);
        Assert.Empty(result.Errors);
    }

    private static DeclarativeAgentService CreateValidationService() => new(
        null!,
        null!,
        null!,
        null!,
        null!,
        null!,
        null!,
        null!,
        null!,
        null!,
        null!,
        null!,
            null!,
        null!,
        new AgentDefinitionParser(),
        new DeclarativeManifestParser(),
        null!);

    private sealed class ExistingChannelRepository : IChannelRepository
    {
        public Task<IReadOnlyList<ChannelConnectionRecord>> ListConnectionsAsync(ChannelConnectionFilter? filter = null, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ChannelConnectionRecord>>([]);

        public Task<ChannelConnectionRecord?> GetConnectionByAsync(ChannelConnectionFilter filter, CancellationToken ct = default) =>
            Task.FromResult<ChannelConnectionRecord?>(new ChannelConnectionRecord
            {
                Id = filter.Id ?? Guid.NewGuid(),
                WorkspaceId = filter.WorkspaceId,
                ChannelType = ChannelType.Slack,
                DisplayName = "Support Slack",
            });

        public Task<ChannelConnectionRecord> CreateConnectionAsync(ChannelConnectionRecord record, CancellationToken ct = default) =>
            Task.FromResult(record);

        public Task<ChannelConnectionRecord?> UpdateConnectionAsync(Guid id, Action<ChannelConnectionRecord> apply, CancellationToken ct = default) =>
            Task.FromResult<ChannelConnectionRecord?>(null);

        public Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<AgentChannelBindingRecord>> ListBindingsAsync(Guid agentId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentChannelBindingRecord>>([]);

        public Task<AgentChannelBindingRecord?> GetBindingByAsync(AgentChannelBindingFilter filter, CancellationToken ct = default) =>
            Task.FromResult<AgentChannelBindingRecord?>(null);

        public Task<AgentChannelBindingRecord> CreateBindingAsync(AgentChannelBindingRecord record, CancellationToken ct = default) =>
            Task.FromResult(record);

        public Task<AgentChannelBindingRecord?> UpdateBindingAsync(Guid bindingId, Action<AgentChannelBindingRecord> apply, CancellationToken ct = default) =>
            Task.FromResult<AgentChannelBindingRecord?>(null);

        public Task<bool> DeleteBindingAsync(Guid bindingId, CancellationToken ct = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<AgentChannelBindingRecord>> FindBindingsByConnectionAsync(Guid connectionId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentChannelBindingRecord>>([]);
    }

    private sealed class ExistingProviderResourceRepository : IProviderResourceRepository
    {
        public Task<IReadOnlyList<ProviderResourceRecord>> ListAsync(Guid workspaceId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ProviderResourceRecord>>([]);

        public Task<ProviderResourceRecord?> GetByNameAsync(Guid workspaceId, string name, CancellationToken ct = default) =>
            Task.FromResult<ProviderResourceRecord?>(new ProviderResourceRecord
            {
                WorkspaceId = workspaceId,
                Name = name,
                Type = name,
                DisplayName = name,
                Models = ["claude-sonnet-4-6"],
                EncryptedCredentialsJson = "{}",
            });

        public Task<ProviderResourceRecord> UpsertAsync(ProviderResourceRecord record, CancellationToken ct = default) =>
            Task.FromResult(record);

        public Task<bool> DeleteAsync(Guid workspaceId, string name, CancellationToken ct = default) =>
            Task.FromResult(false);
    }
}
