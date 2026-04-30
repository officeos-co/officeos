using System.Text.Json;
using ModelContextProtocol.Client;
using Microsoft.Extensions.Logging;

namespace EnterpriseAgentOs.Infrastructure.Features.Mcp;

internal sealed class McpClientManager : IMcpClientManager
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<McpClientManager> _logger;

    public McpClientManager(ILoggerFactory loggerFactory, ILogger<McpClientManager> logger)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public async Task<McpConnectionResult> ConnectAsync(
        McpServerRecord server,
        Dictionary<string, string> credentials,
        CancellationToken ct)
    {
        try
        {
            IClientTransport transport = server.TransportType switch
            {
                McpTransportType.Stdio => CreateStdioTransport(server, credentials),
                McpTransportType.Sse or McpTransportType.StreamableHttp => CreateHttpTransport(server, credentials),
                _ => throw new ArgumentException($"Unsupported transport: {server.TransportType}"),
            };

            var client = await McpClient.CreateAsync(transport, loggerFactory: _loggerFactory, cancellationToken: ct);
            var mcpTools = await client.ListToolsAsync(cancellationToken: ct);

            var tools = mcpTools.Select(t => new McpDiscoveredTool
            {
                Name = t.Name,
                Description = t.Description,
                JsonSchema = t.JsonSchema.ValueKind != JsonValueKind.Undefined
                    ? t.JsonSchema.GetRawText()
                    : null,
                ServerName = server.Name,
                NativeHandle = new McpNativeHandle(t, client),
            }).ToList();

            _logger.LogInformation(
                "Connected to MCP server {Server} ({Transport}), discovered {Count} tools",
                server.Name, server.TransportType, tools.Count);

            return new McpConnectionResult { Tools = tools, Connection = client };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to MCP server {Server}", server.Name);
            return new McpConnectionResult();
        }
    }

    private static StdioClientTransport CreateStdioTransport(McpServerRecord server, Dictionary<string, string> credentials)
    {
        var args = string.IsNullOrEmpty(server.Args)
            ? []
            : JsonSerializer.Deserialize<List<string>>(server.Args) ?? [];

        return new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = server.Command ?? throw new ArgumentException($"MCP server '{server.Name}' has no command"),
            Arguments = args,
            EnvironmentVariables = credentials,
            Name = server.Name,
        });
    }

    private HttpClientTransport CreateHttpTransport(McpServerRecord server, Dictionary<string, string> credentials)
    {
        var url = server.Url ?? throw new ArgumentException($"MCP server '{server.Name}' has no URL");
        return new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(url),
            Name = server.Name,
        }, _loggerFactory);
    }
}

/// <summary>Carries the SDK tool + client reference through the domain boundary.</summary>
internal sealed record McpNativeHandle(McpClientTool Tool, McpClient Client);
