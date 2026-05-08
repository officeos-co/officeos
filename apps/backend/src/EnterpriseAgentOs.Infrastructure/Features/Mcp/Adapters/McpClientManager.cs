using System.Text.Json;
using ModelContextProtocol.Client;
using Microsoft.Extensions.Logging;

namespace EnterpriseAgentOs.Infrastructure.Features.Agents.Integrations;

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
        IntegrationDefinitionRecord server,
        Dictionary<string, string> credentials,
        CancellationToken ct)
    {
        try
        {
            IClientTransport transport = server.TransportType switch
            {
                IntegrationTransportType.Stdio => CreateStdioTransport(server, credentials),
                IntegrationTransportType.Sse or IntegrationTransportType.StreamableHttp => CreateHttpTransport(server, credentials),
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
                IntegrationName = server.Name,
                NativeHandle = new McpNativeHandle(t, client),
            }).ToList();

            _logger.LogInformation(
                "Connected to MCP server {Server} ({Transport}), discovered {Count} tools",
                server.Name, server.TransportType, tools.Count);

            return new McpConnectionResult { Tools = tools, NativeClient = client, Connection = client };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to MCP server {Server}", server.Name);
            return new McpConnectionResult();
        }
    }

    private static StdioClientTransport CreateStdioTransport(IntegrationDefinitionRecord server, Dictionary<string, string> credentials)
    {
        var args = string.IsNullOrEmpty(server.Args)
            ? []
            : JsonSerializer.Deserialize<List<string>>(server.Args) ?? [];

        return new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = server.Command ?? throw new ArgumentException($"MCP server '{server.Name}' has no command"),
            Arguments = ResolveStdioArguments(args),
            EnvironmentVariables = credentials,
            Name = server.Name,
        });
    }

    private static List<string> ResolveStdioArguments(List<string> args)
        => args.Select(ResolveStdioArgument).ToList();

    private static string ResolveStdioArgument(string arg)
    {
        const string scriptPrefix = "eaos://scripts/";
        if (!arg.StartsWith(scriptPrefix, StringComparison.Ordinal))
            return arg;

        var fileName = arg[scriptPrefix.Length..];
        var publishedPath = Path.Combine(AppContext.BaseDirectory, "scripts", fileName);
        if (System.IO.File.Exists(publishedPath))
            return publishedPath;

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var sourcePath = Path.Combine(directory.FullName, "apps", "backend", "scripts", fileName);
            if (System.IO.File.Exists(sourcePath))
                return sourcePath;

            directory = directory.Parent;
        }

        return arg;
    }

    private HttpClientTransport CreateHttpTransport(IntegrationDefinitionRecord server, Dictionary<string, string> credentials)
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
