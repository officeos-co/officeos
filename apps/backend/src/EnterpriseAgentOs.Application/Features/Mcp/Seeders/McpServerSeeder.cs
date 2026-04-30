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
        // ── Developer Tools ─────────────────────────────────────────────

        new()
        {
            Name = "github",
            Title = "GitHub",
            Description = "Interact with GitHub repositories, issues, pull requests, code search, and more",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-github"]""",
            Category = "developer",
            CredentialFieldsJson = """[{"name":"GITHUB_PERSONAL_ACCESS_TOKEN","label":"Personal Access Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "gitlab",
            Title = "GitLab",
            Description = "Manage GitLab repositories, merge requests, issues, and CI/CD pipelines",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-gitlab"]""",
            Category = "developer",
            CredentialFieldsJson = """[{"name":"GITLAB_PERSONAL_ACCESS_TOKEN","label":"Personal Access Token","type":"password","required":true},{"name":"GITLAB_API_URL","label":"GitLab API URL","type":"text","required":false}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "git",
            Title = "Git",
            Description = "Read, search, and manipulate local Git repositories",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-git"]""",
            Category = "developer",
            IsBuiltin = true,
        },
        new()
        {
            Name = "filesystem",
            Title = "Filesystem",
            Description = "Secure file operations with configurable access controls",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-filesystem","/home"]""",
            Category = "developer",
            IsBuiltin = true,
        },
        new()
        {
            Name = "playwright",
            Title = "Playwright",
            Description = "Browser automation for testing — navigate, click, fill forms, take screenshots",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@playwright/mcp@latest"]""",
            Category = "developer",
            IsBuiltin = true,
        },
        new()
        {
            Name = "sentry",
            Title = "Sentry",
            Description = "Access error tracking, performance monitoring, and crash reports from Sentry",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-sentry"]""",
            Category = "developer",
            CredentialFieldsJson = """[{"name":"SENTRY_AUTH_TOKEN","label":"Auth Token","type":"password","required":true},{"name":"SENTRY_ORG","label":"Organization Slug","type":"text","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "linear",
            Title = "Linear",
            Description = "Manage Linear issues, projects, and teams",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","mcp-linear"]""",
            Category = "developer",
            CredentialFieldsJson = """[{"name":"LINEAR_API_KEY","label":"API Key","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "jira",
            Title = "Jira",
            Description = "Search, create, and manage Jira issues and projects",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","mcp-server-jira"]""",
            Category = "developer",
            CredentialFieldsJson = """[{"name":"JIRA_URL","label":"Jira URL","type":"text","required":true},{"name":"JIRA_EMAIL","label":"Email","type":"text","required":true},{"name":"JIRA_API_TOKEN","label":"API Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "postman",
            Title = "Postman",
            Description = "Access and manage Postman collections, environments, and APIs",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@postman/mcp-server"]""",
            Category = "developer",
            CredentialFieldsJson = """[{"name":"POSTMAN_API_KEY","label":"API Key","type":"password","required":true}]""",
            IsBuiltin = true,
        },

        // ── Databases ───────────────────────────────────────────────────

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
            Name = "sqlite",
            Title = "SQLite",
            Description = "Query and manage SQLite databases with built-in analysis",
            TransportType = McpTransportType.Stdio,
            Command = "uvx",
            Args = """["mcp-server-sqlite"]""",
            Category = "database",
            IsBuiltin = true,
        },
        new()
        {
            Name = "redis",
            Title = "Redis",
            Description = "Interact with Redis key-value stores",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-redis"]""",
            Category = "database",
            CredentialFieldsJson = """[{"name":"REDIS_URL","label":"Redis URL","type":"text","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "mysql",
            Title = "MySQL",
            Description = "Query and manage MySQL databases",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","mcp-server-mysql"]""",
            Category = "database",
            CredentialFieldsJson = """[{"name":"MYSQL_HOST","label":"Host","type":"text","required":true},{"name":"MYSQL_USER","label":"User","type":"text","required":true},{"name":"MYSQL_PASSWORD","label":"Password","type":"password","required":true},{"name":"MYSQL_DATABASE","label":"Database","type":"text","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "mongodb",
            Title = "MongoDB",
            Description = "Query and manage MongoDB databases and collections",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","mcp-server-mongodb"]""",
            Category = "database",
            CredentialFieldsJson = """[{"name":"MONGODB_URI","label":"Connection URI","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "supabase",
            Title = "Supabase",
            Description = "Manage Supabase projects — Postgres, auth, storage, and edge functions",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@supabase/mcp-server-supabase@latest"]""",
            Category = "database",
            CredentialFieldsJson = """[{"name":"SUPABASE_ACCESS_TOKEN","label":"Access Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "neo4j",
            Title = "Neo4j",
            Description = "Query and explore Neo4j graph databases",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@neo4j/mcp-neo4j"]""",
            Category = "database",
            CredentialFieldsJson = """[{"name":"NEO4J_URI","label":"URI","type":"text","required":true},{"name":"NEO4J_USERNAME","label":"Username","type":"text","required":true},{"name":"NEO4J_PASSWORD","label":"Password","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "qdrant",
            Title = "Qdrant",
            Description = "Semantic memory and vector search with Qdrant",
            TransportType = McpTransportType.Stdio,
            Command = "uvx",
            Args = """["mcp-server-qdrant"]""",
            Category = "database",
            CredentialFieldsJson = """[{"name":"QDRANT_URL","label":"Qdrant URL","type":"text","required":true},{"name":"QDRANT_API_KEY","label":"API Key","type":"password","required":false}]""",
            IsBuiltin = true,
        },

        // ── Search & Web ────────────────────────────────────────────────

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
            Name = "exa",
            Title = "Exa",
            Description = "AI-native search engine — find content, similar pages, and answers",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","exa-mcp-server"]""",
            Category = "search",
            CredentialFieldsJson = """[{"name":"EXA_API_KEY","label":"API Key","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "tavily",
            Title = "Tavily",
            Description = "AI-optimized search engine designed for AI agents and RAG",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","tavily-mcp@latest"]""",
            Category = "search",
            CredentialFieldsJson = """[{"name":"TAVILY_API_KEY","label":"API Key","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "fetch",
            Title = "Web Fetch",
            Description = "Fetch and convert web content for efficient LLM consumption",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-fetch"]""",
            Category = "search",
            IsBuiltin = true,
        },
        new()
        {
            Name = "firecrawl",
            Title = "Firecrawl",
            Description = "Advanced web scraping and crawling — extract structured data from any website",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","firecrawl-mcp"]""",
            Category = "search",
            CredentialFieldsJson = """[{"name":"FIRECRAWL_API_KEY","label":"API Key","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "perplexity",
            Title = "Perplexity",
            Description = "Real-time web research powered by Perplexity AI",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@anthropic-ai/perplexity-mcp-server"]""",
            Category = "search",
            CredentialFieldsJson = """[{"name":"PERPLEXITY_API_KEY","label":"API Key","type":"password","required":true}]""",
            IsBuiltin = true,
        },

        // ── Communication ───────────────────────────────────────────────

        new()
        {
            Name = "slack",
            Title = "Slack",
            Description = "Read and send messages, manage channels in Slack workspaces",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-slack"]""",
            Category = "communication",
            CredentialFieldsJson = """[{"name":"SLACK_BOT_TOKEN","label":"Bot Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "gmail",
            Title = "Gmail",
            Description = "Read, search, and send emails via Gmail",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@anthropic-ai/gmail-mcp-server"]""",
            Category = "communication",
            CredentialFieldsJson = """[{"name":"GOOGLE_CLIENT_ID","label":"OAuth Client ID","type":"text","required":true},{"name":"GOOGLE_CLIENT_SECRET","label":"OAuth Client Secret","type":"password","required":true},{"name":"GOOGLE_REFRESH_TOKEN","label":"Refresh Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "email",
            Title = "Email (IMAP/SMTP)",
            Description = "Read and send emails via any IMAP/SMTP mail server",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","mcp-server-email"]""",
            Category = "communication",
            CredentialFieldsJson = """[{"name":"IMAP_HOST","label":"IMAP Host","type":"text","required":true},{"name":"IMAP_USER","label":"Email","type":"text","required":true},{"name":"IMAP_PASSWORD","label":"Password","type":"password","required":true},{"name":"SMTP_HOST","label":"SMTP Host","type":"text","required":true}]""",
            IsBuiltin = true,
        },

        // ── Productivity ────────────────────────────────────────────────

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
        new()
        {
            Name = "google-drive",
            Title = "Google Drive",
            Description = "Access, search, and manage files in Google Drive including Docs, Sheets, and Slides",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-gdrive"]""",
            Category = "productivity",
            CredentialFieldsJson = """[{"name":"GOOGLE_CLIENT_ID","label":"OAuth Client ID","type":"text","required":true},{"name":"GOOGLE_CLIENT_SECRET","label":"OAuth Client Secret","type":"password","required":true},{"name":"GOOGLE_REFRESH_TOKEN","label":"Refresh Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "google-calendar",
            Title = "Google Calendar",
            Description = "Manage Google Calendar events — create, update, search, and schedule",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@anthropic-ai/google-calendar-mcp-server"]""",
            Category = "productivity",
            CredentialFieldsJson = """[{"name":"GOOGLE_CLIENT_ID","label":"OAuth Client ID","type":"text","required":true},{"name":"GOOGLE_CLIENT_SECRET","label":"OAuth Client Secret","type":"password","required":true},{"name":"GOOGLE_REFRESH_TOKEN","label":"Refresh Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "google-maps",
            Title = "Google Maps",
            Description = "Geocoding, directions, places search, and distance calculations",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-google-maps"]""",
            Category = "productivity",
            CredentialFieldsJson = """[{"name":"GOOGLE_MAPS_API_KEY","label":"API Key","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "todoist",
            Title = "Todoist",
            Description = "Manage Todoist tasks, projects, and labels",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","mcp-server-todoist"]""",
            Category = "productivity",
            CredentialFieldsJson = """[{"name":"TODOIST_API_TOKEN","label":"API Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "airtable",
            Title = "Airtable",
            Description = "Read and manage Airtable bases, tables, and records",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","mcp-server-airtable"]""",
            Category = "productivity",
            CredentialFieldsJson = """[{"name":"AIRTABLE_API_KEY","label":"API Key","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "trello",
            Title = "Trello",
            Description = "Manage Trello boards, lists, and cards",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","mcp-server-trello"]""",
            Category = "productivity",
            CredentialFieldsJson = """[{"name":"TRELLO_API_KEY","label":"API Key","type":"text","required":true},{"name":"TRELLO_TOKEN","label":"Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "asana",
            Title = "Asana",
            Description = "Manage Asana tasks, projects, and workspaces",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","mcp-server-asana"]""",
            Category = "productivity",
            CredentialFieldsJson = """[{"name":"ASANA_ACCESS_TOKEN","label":"Access Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },

        // ── Cloud Infrastructure ────────────────────────────────────────

        new()
        {
            Name = "aws",
            Title = "AWS",
            Description = "Core AWS capabilities — S3, Lambda, EC2, CloudWatch, and more",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@awslabs/mcp-server-core"]""",
            Category = "cloud",
            CredentialFieldsJson = """[{"name":"AWS_ACCESS_KEY_ID","label":"Access Key ID","type":"text","required":true},{"name":"AWS_SECRET_ACCESS_KEY","label":"Secret Access Key","type":"password","required":true},{"name":"AWS_REGION","label":"Region","type":"text","required":false}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "cloudflare",
            Title = "Cloudflare",
            Description = "Manage Cloudflare Workers, KV, R2, and D1 resources",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@cloudflare/mcp-server-cloudflare"]""",
            Category = "cloud",
            CredentialFieldsJson = """[{"name":"CLOUDFLARE_API_TOKEN","label":"API Token","type":"password","required":true},{"name":"CLOUDFLARE_ACCOUNT_ID","label":"Account ID","type":"text","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "vercel",
            Title = "Vercel",
            Description = "Manage Vercel deployments, projects, and domains",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","mcp-server-vercel"]""",
            Category = "cloud",
            CredentialFieldsJson = """[{"name":"VERCEL_API_TOKEN","label":"API Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "kubernetes",
            Title = "Kubernetes",
            Description = "Manage Kubernetes clusters — pods, deployments, services, and logs",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","mcp-server-kubernetes"]""",
            Category = "cloud",
            IsBuiltin = true,
        },
        new()
        {
            Name = "terraform",
            Title = "Terraform",
            Description = "Manage Terraform configurations, plans, and state",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","mcp-server-terraform"]""",
            Category = "cloud",
            IsBuiltin = true,
        },
        new()
        {
            Name = "docker",
            Title = "Docker",
            Description = "Manage Docker containers, images, volumes, and networks",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","mcp-server-docker"]""",
            Category = "cloud",
            IsBuiltin = true,
        },

        // ── Monitoring & Observability ───────────────────────────────────

        new()
        {
            Name = "grafana",
            Title = "Grafana",
            Description = "Query Grafana dashboards, datasources, and alerts",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@grafana/mcp-grafana"]""",
            Category = "monitoring",
            CredentialFieldsJson = """[{"name":"GRAFANA_URL","label":"Grafana URL","type":"text","required":true},{"name":"GRAFANA_API_KEY","label":"API Key","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "datadog",
            Title = "Datadog",
            Description = "Query Datadog metrics, logs, and monitors",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","mcp-server-datadog"]""",
            Category = "monitoring",
            CredentialFieldsJson = """[{"name":"DD_API_KEY","label":"API Key","type":"password","required":true},{"name":"DD_APP_KEY","label":"Application Key","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "pagerduty",
            Title = "PagerDuty",
            Description = "Manage PagerDuty incidents, services, and on-call schedules",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","mcp-server-pagerduty"]""",
            Category = "monitoring",
            CredentialFieldsJson = """[{"name":"PAGERDUTY_API_KEY","label":"API Key","type":"password","required":true}]""",
            IsBuiltin = true,
        },

        // ── Finance & Payments ──────────────────────────────────────────

        new()
        {
            Name = "stripe",
            Title = "Stripe",
            Description = "Manage Stripe payments, customers, subscriptions, and invoices",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@stripe/mcp@latest"]""",
            Category = "finance",
            CredentialFieldsJson = """[{"name":"STRIPE_SECRET_KEY","label":"Secret Key","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "paypal",
            Title = "PayPal",
            Description = "Manage PayPal transactions, invoices, and payment links",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@paypal/mcp@latest"]""",
            Category = "finance",
            CredentialFieldsJson = """[{"name":"PAYPAL_CLIENT_ID","label":"Client ID","type":"text","required":true},{"name":"PAYPAL_CLIENT_SECRET","label":"Client Secret","type":"password","required":true}]""",
            IsBuiltin = true,
        },

        // ── CRM & Sales ─────────────────────────────────────────────────

        new()
        {
            Name = "hubspot",
            Title = "HubSpot",
            Description = "Manage HubSpot contacts, deals, companies, and tickets",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","mcp-server-hubspot"]""",
            Category = "crm",
            CredentialFieldsJson = """[{"name":"HUBSPOT_ACCESS_TOKEN","label":"Access Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "salesforce",
            Title = "Salesforce",
            Description = "Query and manage Salesforce objects, reports, and dashboards",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","mcp-server-salesforce"]""",
            Category = "crm",
            CredentialFieldsJson = """[{"name":"SALESFORCE_INSTANCE_URL","label":"Instance URL","type":"text","required":true},{"name":"SALESFORCE_ACCESS_TOKEN","label":"Access Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },

        // ── AI & Knowledge ──────────────────────────────────────────────

        new()
        {
            Name = "memory",
            Title = "Memory",
            Description = "Knowledge graph-based persistent memory for LLMs",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-memory"]""",
            Category = "ai",
            IsBuiltin = true,
        },
        new()
        {
            Name = "sequential-thinking",
            Title = "Sequential Thinking",
            Description = "Dynamic and reflective problem-solving through structured thought sequences",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-sequentialthinking"]""",
            Category = "ai",
            IsBuiltin = true,
        },
        new()
        {
            Name = "context7",
            Title = "Context7",
            Description = "Up-to-date documentation for any library directly in your AI prompts",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@upstash/context7-mcp@latest"]""",
            Category = "ai",
            IsBuiltin = true,
        },

        // ── CI/CD & DevOps ──────────────────────────────────────────────

        new()
        {
            Name = "circleci",
            Title = "CircleCI",
            Description = "Analyze and fix CircleCI pipeline failures",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@circleci/mcp-server-circleci"]""",
            Category = "devops",
            CredentialFieldsJson = """[{"name":"CIRCLECI_TOKEN","label":"API Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "buildkite",
            Title = "Buildkite",
            Description = "Manage Buildkite pipelines, builds, and agents",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@buildkite/mcp-server"]""",
            Category = "devops",
            CredentialFieldsJson = """[{"name":"BUILDKITE_API_TOKEN","label":"API Token","type":"password","required":true}]""",
            IsBuiltin = true,
        },

        // ── Utilities ───────────────────────────────────────────────────

        new()
        {
            Name = "time",
            Title = "Time",
            Description = "Time and timezone conversion utilities",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-time"]""",
            Category = "utility",
            IsBuiltin = true,
        },
        new()
        {
            Name = "e2b",
            Title = "E2B Code Sandbox",
            Description = "Execute code in secure cloud sandboxes — Python, JS, and more",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@e2b/mcp-server"]""",
            Category = "utility",
            CredentialFieldsJson = """[{"name":"E2B_API_KEY","label":"API Key","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "elevenlabs",
            Title = "ElevenLabs",
            Description = "Text-to-speech synthesis with natural-sounding AI voices",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@elevenlabs/mcp-server"]""",
            Category = "utility",
            CredentialFieldsJson = """[{"name":"ELEVENLABS_API_KEY","label":"API Key","type":"password","required":true}]""",
            IsBuiltin = true,
        },
        new()
        {
            Name = "puppeteer",
            Title = "Puppeteer",
            Description = "Browser automation — navigate pages, take screenshots, interact with web content",
            TransportType = McpTransportType.Stdio,
            Command = "npx",
            Args = """["-y","@modelcontextprotocol/server-puppeteer"]""",
            Category = "utility",
            IsBuiltin = true,
        },
    ];
}
