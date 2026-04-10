using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.Skills;

public sealed record CredentialField(
    string Key,
    string Label,
    string Kind, // "password" | "text" | "textarea"
    bool Required = true,
    string? Placeholder = null,
    string? Help = null);

public sealed record LlmToolSpec(
    string Name,
    string Description,
    JsonElement Parameters);

public sealed record SkillManifest(
    string Name,
    string Title,
    string Description,
    string Emoji,
    IReadOnlyList<CredentialField> CredentialFields,
    IReadOnlyList<LlmToolSpec> LlmTools);

/// <summary>
/// Static in-code registry of first-party skills. Mirror of v1's
/// <c>app.skills.__init__</c> + the three <c>{notion,github,google}/__init__.py</c>
/// MANIFEST declarations. Adding a skill here requires a matching
/// implementation class and controller action wiring — no DB seed.
/// </summary>
public static class SkillManifests
{
    private static JsonElement Schema(string json) =>
        JsonDocument.Parse(json).RootElement.Clone();

    public static IReadOnlyDictionary<string, SkillManifest> All { get; } =
        new Dictionary<string, SkillManifest>(StringComparer.OrdinalIgnoreCase)
        {
            ["notion"] = new SkillManifest(
                Name: "notion",
                Title: "Notion",
                Description: "Search and read Notion pages and databases via the Notion REST API.",
                Emoji: "📝",
                CredentialFields: new[]
                {
                    new CredentialField(
                        Key: "api_key",
                        Label: "Internal Integration Token",
                        Kind: "password",
                        Placeholder: "secret_…",
                        Help: "Create an Internal Integration at https://www.notion.so/my-integrations and share the pages you want the agent to access with it."),
                },
                LlmTools: new[]
                {
                    new LlmToolSpec(
                        Name: "notion.search",
                        Description: "Search the connected Notion workspace for pages matching a query.",
                        Parameters: Schema("""
                            {
                              "type": "object",
                              "properties": {
                                "query": { "type": "string", "description": "Free-text query." },
                                "page_size": { "type": "integer", "minimum": 1, "maximum": 100, "default": 10 }
                              },
                              "required": ["query"]
                            }
                            """)),
                    new LlmToolSpec(
                        Name: "notion.read_page",
                        Description: "Fetch a Notion page's top-level block children as plain text.",
                        Parameters: Schema("""
                            {
                              "type": "object",
                              "properties": {
                                "page_id": { "type": "string" }
                              },
                              "required": ["page_id"]
                            }
                            """)),
                }),

            ["github"] = new SkillManifest(
                Name: "github",
                Title: "GitHub",
                Description: "List repositories, issues, and pull requests via the GitHub REST API.",
                Emoji: "🐙",
                CredentialFields: new[]
                {
                    new CredentialField(
                        Key: "token",
                        Label: "Personal Access Token",
                        Kind: "password",
                        Placeholder: "github_pat_… or ghp_…",
                        Help: "Fine-grained PAT recommended. Needs read access to the repos/issues/PRs you want the agent to see. Create one at https://github.com/settings/tokens."),
                },
                LlmTools: new[]
                {
                    new LlmToolSpec(
                        Name: "github.list_repos",
                        Description: "List repositories accessible to the authenticated user.",
                        Parameters: Schema("""
                            {
                              "type": "object",
                              "properties": {
                                "visibility": { "type": "string", "enum": ["all", "public", "private"], "default": "all" },
                                "per_page": { "type": "integer", "minimum": 1, "maximum": 100, "default": 30 }
                              }
                            }
                            """)),
                    new LlmToolSpec(
                        Name: "github.list_issues",
                        Description: "List open issues in a single repository.",
                        Parameters: Schema("""
                            {
                              "type": "object",
                              "properties": {
                                "owner": { "type": "string" },
                                "repo": { "type": "string" },
                                "state": { "type": "string", "enum": ["open", "closed", "all"], "default": "open" }
                              },
                              "required": ["owner", "repo"]
                            }
                            """)),
                    new LlmToolSpec(
                        Name: "github.list_prs",
                        Description: "List pull requests in a single repository.",
                        Parameters: Schema("""
                            {
                              "type": "object",
                              "properties": {
                                "owner": { "type": "string" },
                                "repo": { "type": "string" },
                                "state": { "type": "string", "enum": ["open", "closed", "all"], "default": "open" }
                              },
                              "required": ["owner", "repo"]
                            }
                            """)),
                }),

            ["google"] = new SkillManifest(
                Name: "google",
                Title: "Google Workspace",
                Description: "Search Google Drive and list upcoming Calendar events via a service-account key.",
                Emoji: "🔎",
                CredentialFields: new[]
                {
                    new CredentialField(
                        Key: "service_account_json",
                        Label: "Service Account JSON",
                        Kind: "textarea",
                        Placeholder: """{ "type": "service_account", "project_id": "...", "private_key": "-----BEGIN PRIVATE KEY-----..." }""",
                        Help: "Paste the entire contents of a service-account key file. Enable the Drive and Calendar APIs in your GCP project and share the relevant Drive folders / Calendar with the service-account email."),
                    new CredentialField(
                        Key: "calendar_id",
                        Label: "Calendar ID (optional)",
                        Kind: "text",
                        Required: false,
                        Placeholder: "primary",
                        Help: "Defaults to 'primary'. Use the calendar's email address for shared calendars."),
                },
                LlmTools: new[]
                {
                    new LlmToolSpec(
                        Name: "google.drive_search",
                        Description: "Search Google Drive for files whose name contains the query.",
                        Parameters: Schema("""
                            {
                              "type": "object",
                              "properties": {
                                "query": { "type": "string" },
                                "page_size": { "type": "integer", "minimum": 1, "maximum": 100, "default": 10 }
                              },
                              "required": ["query"]
                            }
                            """)),
                    new LlmToolSpec(
                        Name: "google.calendar_upcoming",
                        Description: "List the next N events on the configured calendar.",
                        Parameters: Schema("""
                            {
                              "type": "object",
                              "properties": {
                                "max_results": { "type": "integer", "minimum": 1, "maximum": 50, "default": 10 }
                              }
                            }
                            """)),
                }),
        };

    public static SkillManifest? For(string name) =>
        All.TryGetValue(name, out var m) ? m : null;
}
