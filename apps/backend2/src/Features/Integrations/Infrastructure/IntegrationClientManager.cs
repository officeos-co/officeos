namespace OffceOs.Infrastructure.Features.Integrations;

internal sealed class IntegrationClientManager : IIntegrationClientManager
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<IntegrationClientManager> _logger;

    public IntegrationClientManager(ILoggerFactory loggerFactory, ILogger<IntegrationClientManager> logger)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public async Task<IntegrationConnectionResult> ConnectAsync(
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
            var integrationTools = await client.ListToolsAsync(cancellationToken: ct);

            var tools = integrationTools.Select(t => new IntegrationDiscoveredTool
            {
                Name = t.Name,
                Description = t.Description,
                Parameters = t.JsonSchema.ValueKind != JsonValueKind.Undefined
                    ? t.JsonSchema
                    : null,
                IntegrationName = server.Name,
                NativeHandle = new IntegrationNativeHandle(t, client),
            }).ToList();

            _logger.LogInformation(
                "Connected to integration {Server} ({Transport}), discovered {Count} tools",
                server.Name, server.TransportType, tools.Count);

            return new IntegrationConnectionResult { Tools = tools, NativeClient = client, Connection = client };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to integration {Server}", server.Name);
            return new IntegrationConnectionResult();
        }
    }

    private static StdioClientTransport CreateStdioTransport(IntegrationDefinitionRecord server, Dictionary<string, string> credentials)
    {
        var args = string.IsNullOrEmpty(server.Args)
            ? []
            : JsonSerializer.Deserialize<List<string>>(server.Args) ?? [];

        return new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = server.Command ?? throw new ArgumentException($"integration '{server.Name}' has no command"),
            Arguments = ResolveStdioArguments(args),
            EnvironmentVariables = credentials.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value),
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
        var publishedPath = System.IO.Path.Combine(AppContext.BaseDirectory, "scripts", fileName);
        if (System.IO.File.Exists(publishedPath))
            return publishedPath;

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var sourcePath = System.IO.Path.Combine(directory.FullName, "apps", "backend", "scripts", fileName);
            if (System.IO.File.Exists(sourcePath))
                return sourcePath;

            directory = directory.Parent;
        }

        return arg;
    }

    private HttpClientTransport CreateHttpTransport(IntegrationDefinitionRecord server, Dictionary<string, string> credentials)
    {
        var url = server.Url ?? throw new ArgumentException($"integration '{server.Name}' has no URL");
        return new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(url),
            Name = server.Name,
        }, _loggerFactory);
    }
}

/// <summary>Carries the SDK tool + client reference through the domain boundary.</summary>
internal sealed record IntegrationNativeHandle(McpClientTool Tool, McpClient Client);
