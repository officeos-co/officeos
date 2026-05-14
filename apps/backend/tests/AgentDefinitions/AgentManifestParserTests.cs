using OffceOs.Application.Features.AgentDefinitions;
using Xunit;

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
}
