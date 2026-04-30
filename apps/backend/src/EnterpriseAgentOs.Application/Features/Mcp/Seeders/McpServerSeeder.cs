using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnterpriseAgentOs.Application.Features.Mcp;

public static class McpServerSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var repo = services.GetRequiredService<IMcpServerRepository>();
        var logger = services.GetRequiredService<ILogger<McpServerService>>();

        var servers = GetBuiltinServers();
        foreach (var server in servers)
        {
            await repo.UpsertAsync(server);
        }

        logger.LogInformation("Seeded {Count} built-in MCP servers", servers.Count);
    }

    private static List<McpServerRecord> GetBuiltinServers() =>
    [
        new()
        {
            Name = "filesystem",
            Title = "Filesystem",
            Description = "Read and write files on the local filesystem",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-filesystem","/home"]""",
            Category = "developer",
            IsBuiltin = true,
        },
        new()
        {
            Name = "github",
            Title = "GitHub",
            Description = "Interact with GitHub repositories, issues, pull requests, and more",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-github"]""",
            Category = "developer",
            CredentialFieldsJson = """[{"name":"GITHUB_PERSONAL_ACCESS_TOKEN","label":"Personal Access Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "postgres",
            Title = "PostgreSQL",
            Description = "Query and manage PostgreSQL databases",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-postgres"]""",
            Category = "database",
            CredentialFieldsJson = """[{"name":"POSTGRES_CONNECTION_STRING","label":"Connection String","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "brave-search",
            Title = "Brave Search",
            Description = "Search the web using Brave Search API",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-brave-search"]""",
            Category = "search",
            CredentialFieldsJson = """[{"name":"BRAVE_API_KEY","label":"API Key","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "slack",
            Title = "Slack",
            Description = "Read and send messages in Slack workspaces",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-slack"]""",
            Category = "communication",
            CredentialFieldsJson = """[{"name":"SLACK_BOT_TOKEN","label":"Bot Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "google-drive",
            Title = "Google Drive",
            Description = "Access and manage files in Google Drive",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-gdrive"]""",
            Category = "productivity",
            IsBuiltin = true,
        },
        new()
        {
            Name = "notion",
            Title = "Notion",
            Description = "Search and manage Notion pages, databases, and blocks",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@notionhq/notion-mcp-server"]""",
            Category = "productivity",
            CredentialFieldsJson = """[{"name":"OPENAPI_MCP_HEADERS","label":"Integration Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },
    ];
}
