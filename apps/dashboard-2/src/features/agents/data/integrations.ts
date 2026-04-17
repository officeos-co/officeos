export type Tool = {
  name: string
  description: string
}

export type CredentialField = {
  key: string
  label: string
  type: "password" | "text"
  placeholder: string
}

export type Integration = {
  name: string
  slug: string
  logo: string
  description: string
  likes: number
  updatedAgo: string
  tools: Tool[]
  credentials: CredentialField[]
  added: boolean
  skillMd: string
}

export const builtInTools: Tool[] = [
  { name: "bash", description: "Execute bash commands" },
  { name: "read", description: "Read files" },
  { name: "write", description: "Write files" },
  { name: "edit", description: "String replacement in files" },
  { name: "glob", description: "File pattern matching" },
  { name: "grep", description: "Text search with regex" },
  { name: "web_fetch", description: "Fetch URL content" },
  { name: "web_search", description: "Search the web" },
]

const SOURCE_BASE = "https://github.com/officeos/integrations/tree/main/packages"

export function sourceUrl(slug: string) {
  return `${SOURCE_BASE}/${slug}`
}

export const integrations: Integration[] = [
  {
    name: "GitHub",
    slug: "github",
    logo: "/logos/github.svg",
    description: "Create issues, pull requests, manage repositories, and automate workflows.",
    likes: 342,
    updatedAgo: "2 days ago",
    added: true,
    credentials: [{ key: "GITHUB_TOKEN", label: "Personal Access Token", type: "password", placeholder: "ghp_..." }],
    tools: [
      { name: "create_issue", description: "Create a new issue in a repository" },
      { name: "create_pr", description: "Open a pull request from a branch" },
      { name: "get_file", description: "Read the contents of a file" },
      { name: "list_repos", description: "List accessible repositories" },
    ],
    skillMd: `# GitHub

Create issues, manage pull requests, and read repository files.

All commands go through \`skill_exec\` using CLI-style syntax.

## Commands

### Create issue

\`\`\`
github create_issue --repo "acme/api" --title "Bug: login broken" --body "Steps to reproduce..." --labels "bug"
\`\`\`

| Argument | Type   | Required | Default | Description                    |
| -------- | ------ | -------- | ------- | ------------------------------ |
| \`repo\`   | string | yes      |         | Owner/repo (e.g. \`acme/api\`)   |
| \`title\`  | string | yes      |         | Issue title                    |
| \`body\`   | string | no       |         | Markdown body                  |
| \`labels\` | string | no       |         | Comma-separated labels         |

### Create pull request

\`\`\`
github create_pr --repo "acme/api" --title "fix: auth redirect" --head "fix/auth" --base "main"
\`\`\`

| Argument | Type   | Required | Default | Description      |
| -------- | ------ | -------- | ------- | ---------------- |
| \`repo\`   | string | yes      |         | Owner/repo       |
| \`title\`  | string | yes      |         | PR title         |
| \`head\`   | string | yes      |         | Source branch    |
| \`base\`   | string | no       | main    | Target branch    |

### Get file

\`\`\`
github get_file --repo "acme/api" --path "src/index.ts"
\`\`\`

| Argument | Type   | Required | Default | Description         |
| -------- | ------ | -------- | ------- | ------------------- |
| \`repo\`   | string | yes      |         | Owner/repo          |
| \`path\`   | string | yes      |         | File path           |
| \`ref\`    | string | no       | HEAD    | Branch or commit    |

### List repos

\`\`\`
github list_repos --page_size 20
\`\`\`

## Authentication

Requires a GitHub Personal Access Token with \`repo\` scope.
`,
  },
  {
    name: "Browser",
    slug: "browser",
    logo: "/logos/browser.svg",
    description: "Search the web and scrape pages for up-to-date information.",
    likes: 218,
    updatedAgo: "5 days ago",
    added: true,
    credentials: [],
    tools: [
      { name: "search", description: "Search the web with a query" },
      { name: "scrape_url", description: "Scrape and extract page content" },
    ],
    skillMd: `# Browser

Search the web and scrape any public page for up-to-date information.

## Commands

### Search

\`\`\`
browser search --query "latest Next.js release"
\`\`\`

| Argument | Type   | Required | Default | Description   |
| -------- | ------ | -------- | ------- | ------------- |
| \`query\`  | string | yes      |         | Search query  |

Returns top results with title, URL, and snippet.

### Scrape URL

\`\`\`
browser scrape_url --url "https://nextjs.org/blog"
\`\`\`

| Argument | Type   | Required | Default | Description    |
| -------- | ------ | -------- | ------- | -------------- |
| \`url\`    | string | yes      |         | URL to scrape  |

Returns cleaned text content, stripping navigation and ads.

## Authentication

No credentials required.
`,
  },
  {
    name: "Google Workspace",
    slug: "google-workspace",
    logo: "/logos/google-drive.svg",
    description: "Search Google Drive files and list upcoming Google Calendar events.",
    likes: 156,
    updatedAgo: "1 week ago",
    added: false,
    credentials: [{ key: "GOOGLE_SERVICE_ACCOUNT", label: "Service Account JSON", type: "text", placeholder: '{"type":"service_account",...}' }],
    tools: [
      { name: "drive_search", description: "Search Drive files by name" },
      { name: "calendar_upcoming", description: "List upcoming calendar events" },
    ],
    skillMd: `# Google Workspace

Search Google Drive files and list upcoming Google Calendar events.

All commands go through \`skill_exec\` using CLI-style syntax.
Use \`--help\` at any level to discover actions and arguments.

## Commands

### Search Drive

\`\`\`
google drive_search --query "Q1 report" --page_size 5
\`\`\`

| Argument    | Type   | Required | Default | Description               |
| ----------- | ------ | -------- | ------- | ------------------------- |
| \`query\`     | string | yes      |         | Search text (file names)  |
| \`page_size\` | int    | no       | 10      | Results to return (1–100) |

Returns array of files: \`id\`, \`name\`, \`mime_type\`, \`web_view_link\`, \`modified_time\`.
The \`web_view_link\` opens the file in a browser.

### Upcoming calendar events

\`\`\`
google calendar_upcoming --max_results 5
\`\`\`

| Argument      | Type | Required | Default | Description             |
| ------------- | ---- | -------- | ------- | ----------------------- |
| \`max_results\` | int  | no       | 10      | Events to return (1–50) |

Returns array of events: \`id\`, \`summary\`, \`start\`, \`end\`, \`html_link\`.

## Workflow

For file/document questions:

1. Use \`google drive_search\` with descriptive keywords.
2. Present results with file names and direct links (\`web_view_link\`).
3. If results are empty, try shorter or alternative keywords.

For schedule/meeting questions:

1. Use \`google calendar_upcoming\` to get the next events.
2. Summarize events by date, time, and title.
3. Include the \`html_link\` so the user can open events directly.

## Safety notes

- Drive search matches **file names only** — no content search.
- Calendar returns events from the configured calendar only (defaults to primary).
- Only files and calendars shared with the configured service account are visible.
- This skill is **read-only**. You cannot create, modify, or delete files or events.
`,
  },
  {
    name: "Notion",
    slug: "notion",
    logo: "/logos/notion.svg",
    description: "Read and write Notion pages, databases, and blocks.",
    likes: 267,
    updatedAgo: "4 days ago",
    added: true,
    credentials: [{ key: "NOTION_TOKEN", label: "Integration Token", type: "password", placeholder: "secret_..." }],
    tools: [
      { name: "search", description: "Search pages and databases" },
      { name: "create_page", description: "Create a new page" },
      { name: "append_blocks", description: "Append content blocks to a page" },
    ],
    skillMd: `# Notion

Connect agents to your Notion workspace — search pages, create new ones, and append content.

## Commands

### Search pages

\`\`\`
notion search --query "meeting notes" --page_size 5
\`\`\`

| Argument    | Type   | Required | Default | Description              |
| ----------- | ------ | -------- | ------- | ------------------------ |
| \`query\`     | string | yes      |         | Search text              |
| \`page_size\` | int    | no       | 10      | Results to return        |

### Create page

\`\`\`
notion create_page --parent "Page ID" --title "New page" --content "Hello world"
\`\`\`

| Argument  | Type   | Required | Default | Description           |
| --------- | ------ | -------- | ------- | --------------------- |
| \`parent\`  | string | yes      |         | Parent page or DB ID  |
| \`title\`   | string | yes      |         | Page title            |
| \`content\` | string | no       |         | Initial text content  |

### Append blocks

\`\`\`
notion append_blocks --page "Page ID" --blocks "[{\\"type\\":\\"paragraph\\",\\"text\\":\\"Hello\\"}]"
\`\`\`

## Authentication

Requires a Notion integration token. Create one at notion.so/my-integrations.
`,
  },
  {
    name: "Linear",
    slug: "linear",
    logo: "/logos/linear.svg",
    description: "Manage issues, projects, and cycles in Linear.",
    likes: 198,
    updatedAgo: "6 days ago",
    added: false,
    credentials: [{ key: "LINEAR_API_KEY", label: "API Key", type: "password", placeholder: "lin_api_..." }],
    tools: [
      { name: "create_issue", description: "Create a new issue" },
      { name: "list_issues", description: "List issues with filters" },
      { name: "update_issue", description: "Update an existing issue" },
    ],
    skillMd: `# Linear

Create, update, and query issues in Linear project management.

## Commands

### Create issue

\`\`\`
linear create_issue --team "ENG" --title "Fix auth bug" --priority "high"
\`\`\`

| Argument   | Type   | Required | Default | Description                        |
| ---------- | ------ | -------- | ------- | ---------------------------------- |
| \`team\`     | string | yes      |         | Team key (e.g. ENG)                |
| \`title\`    | string | yes      |         | Issue title                        |
| \`priority\` | string | no       | medium  | urgent, high, medium, low, none    |

### List issues

\`\`\`
linear list_issues --team "ENG" --status "In Progress" --limit 20
\`\`\`

### Update issue

\`\`\`
linear update_issue --id "ENG-123" --status "Done"
\`\`\`

## Authentication

Requires a Linear API key.
`,
  },
  {
    name: "Jira",
    slug: "jira",
    logo: "/logos/jira.svg",
    description: "Create and manage Jira issues, sprints, and boards.",
    likes: 87,
    updatedAgo: "1 week ago",
    added: false,
    credentials: [
      { key: "JIRA_URL", label: "Jira URL", type: "text", placeholder: "https://your-org.atlassian.net" },
      { key: "JIRA_EMAIL", label: "Email", type: "text", placeholder: "you@company.com" },
      { key: "JIRA_API_TOKEN", label: "API Token", type: "password", placeholder: "" },
    ],
    tools: [
      { name: "create_issue", description: "Create a new Jira issue" },
      { name: "search", description: "Search issues with JQL" },
    ],
    skillMd: `# Jira

Create issues and search with JQL in Jira.

## Commands

### Create issue

\`\`\`
jira create_issue --project "PROJ" --summary "Bug report" --type "Bug"
\`\`\`

| Argument  | Type   | Required | Default | Description    |
| --------- | ------ | -------- | ------- | -------------- |
| \`project\` | string | yes      |         | Project key    |
| \`summary\` | string | yes      |         | Issue summary  |
| \`type\`    | string | no       | Task    | Issue type     |

### Search issues

\`\`\`
jira search --jql "project = PROJ AND status = Open" --limit 20
\`\`\`

## Authentication

Requires Jira URL, email, and API token.
`,
  },
  {
    name: "HubSpot",
    slug: "hubspot",
    logo: "/logos/hubspot.svg",
    description: "Manage contacts, deals, and CRM pipelines in HubSpot.",
    likes: 95,
    updatedAgo: "3 days ago",
    added: false,
    credentials: [{ key: "HUBSPOT_TOKEN", label: "Private App Token", type: "password", placeholder: "pat-..." }],
    tools: [
      { name: "create_contact", description: "Create a new contact" },
      { name: "list_contacts", description: "List contacts with filters" },
      { name: "create_deal", description: "Create a new deal" },
      { name: "search", description: "Search across CRM objects" },
    ],
    skillMd: `# HubSpot

Manage your CRM — create contacts, deals, and search across objects.

## Commands

### Create contact

\`\`\`
hubspot create_contact --email "john@example.com" --firstname "John" --lastname "Doe"
\`\`\`

### Search CRM

\`\`\`
hubspot search --object "contacts" --query "john" --limit 10
\`\`\`

### Create deal

\`\`\`
hubspot create_deal --name "Enterprise deal" --pipeline "default" --stage "appointment"
\`\`\`

## Authentication

Requires a HubSpot private app access token.
`,
  },
  {
    name: "Salesforce",
    slug: "salesforce",
    logo: "/logos/salesforce.svg",
    description: "Query records, manage objects, and automate Salesforce workflows.",
    likes: 73,
    updatedAgo: "5 days ago",
    added: false,
    credentials: [
      { key: "SF_CLIENT_ID", label: "Client ID", type: "text", placeholder: "" },
      { key: "SF_CLIENT_SECRET", label: "Client Secret", type: "password", placeholder: "" },
      { key: "SF_REFRESH_TOKEN", label: "Refresh Token", type: "password", placeholder: "" },
    ],
    tools: [
      { name: "query", description: "Execute a SOQL query" },
      { name: "create_record", description: "Create a new record" },
      { name: "update_record", description: "Update an existing record" },
    ],
    skillMd: `# Salesforce

Query and manage Salesforce CRM records using SOQL.

## Commands

### Query

\`\`\`
salesforce query --soql "SELECT Id, Name FROM Account WHERE Industry = 'Tech' LIMIT 10"
\`\`\`

| Argument | Type   | Required | Default | Description   |
| -------- | ------ | -------- | ------- | ------------- |
| \`soql\`   | string | yes      |         | SOQL query    |

### Create record

\`\`\`
salesforce create_record --object "Account" --fields '{"Name":"Acme","Industry":"Tech"}'
\`\`\`

### Update record

\`\`\`
salesforce update_record --object "Account" --id "001..." --fields '{"Industry":"SaaS"}'
\`\`\`

## Authentication

Requires Salesforce connected app credentials.
`,
  },
]
