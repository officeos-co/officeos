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
            apiVersion: eaos.dev/v1
            kind: Channel
            metadata:
              name: support-slack
            spec:
              type: slack
              token: xoxb-test
            ---
            apiVersion: eaos.dev/v1
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
            apiVersion: eaos.dev/v1
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
}
