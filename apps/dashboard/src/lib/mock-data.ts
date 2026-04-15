import type { Agent } from "@/types/agent";
import type { Skill } from "@/types/skill";
import type { Provider } from "@/types/provider";
import type { Runner } from "@/types/runner";
import type { SystemEvent } from "@/hooks/useSystemEvents";
import type { ApprovalRequest } from "@/hooks/useApprovalRequests";
import type { User } from "@/types/auth";

export const MOCK_USER: User = {
  id: "usr_mock_001",
  email: "harro@officeos.co",
  name: "Harro Krog",
  avatarUrl: null,
};

export const MOCK_AGENTS: Agent[] = [
  {
    id: "agt_a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    name: "Research Assistant",
    provider: "anthropic",
    model: "claude-sonnet-4-6",
    status: "running",
    podName: "agent-research-assistant-7f8d9",
    serviceUrl: "http://agent-research-assistant:8080",
    createdAt: new Date(Date.now() - 2 * 86400000).toISOString(),
  },
  {
    id: "agt_b2c3d4e5-f6a7-8901-bcde-f12345678901",
    name: "Code Reviewer",
    provider: "anthropic",
    model: "claude-opus-4-6",
    status: "running",
    podName: "agent-code-reviewer-3a4b5",
    serviceUrl: "http://agent-code-reviewer:8080",
    createdAt: new Date(Date.now() - 5 * 86400000).toISOString(),
  },
  {
    id: "agt_c3d4e5f6-a7b8-9012-cdef-123456789012",
    name: "Customer Support Bot",
    provider: "google",
    model: "gemini-2.5-pro",
    status: "running",
    podName: "agent-support-bot-9c0d1",
    serviceUrl: "http://agent-support-bot:8080",
    createdAt: new Date(Date.now() - 10 * 86400000).toISOString(),
  },
  {
    id: "agt_d4e5f6a7-b8c9-0123-defa-234567890123",
    name: "Data Pipeline Monitor",
    provider: "anthropic",
    model: "claude-haiku-4-5-20251001",
    status: "pending",
    podName: null,
    serviceUrl: null,
    createdAt: new Date(Date.now() - 1 * 86400000).toISOString(),
  },
  {
    id: "agt_e5f6a7b8-c9d0-1234-efab-345678901234",
    name: "Content Writer",
    provider: "openai",
    model: "gpt-4o",
    status: "stopped",
    podName: null,
    serviceUrl: null,
    createdAt: new Date(Date.now() - 14 * 86400000).toISOString(),
  },
  {
    id: "agt_f6a7b8c9-d0e1-2345-fabc-456789012345",
    name: "Security Scanner",
    provider: "anthropic",
    model: "claude-sonnet-4-6",
    status: "failed",
    podName: "agent-security-scanner-2e3f4",
    serviceUrl: null,
    createdAt: new Date(Date.now() - 3 * 86400000).toISOString(),
  },
];

export const MOCK_SKILLS: Skill[] = [
  {
    name: "github",
    title: "GitHub",
    description: "Create issues, pull requests, manage repositories, and automate workflows.",
    emoji: "🐙",
    installed: true,
    configured: true,
    runTarget: "cloud",
    credentialFields: [
      { key: "GITHUB_TOKEN", label: "GitHub Token", kind: "password", required: true, placeholder: "ghp_...", help: "Personal access token with repo scope" },
    ],
    llmTools: [
      { name: "create_issue", description: "Create a GitHub issue", parameters: {} },
      { name: "create_pr", description: "Create a pull request", parameters: {} },
      { name: "list_repos", description: "List repositories", parameters: {} },
      { name: "get_file", description: "Read a file from a repository", parameters: {} },
    ],
    registryVersion: "1.4.0",
    registryStatus: "active",
  },
  {
    name: "linear",
    title: "Linear",
    description: "Manage issues, projects, and cycles in Linear project management.",
    emoji: "📐",
    installed: true,
    configured: true,
    runTarget: "cloud",
    credentialFields: [
      { key: "LINEAR_API_KEY", label: "Linear API Key", kind: "password", required: true, placeholder: "lin_api_...", help: null },
    ],
    llmTools: [
      { name: "create_issue", description: "Create a Linear issue", parameters: {} },
      { name: "update_issue", description: "Update an issue", parameters: {} },
      { name: "list_issues", description: "List issues with filters", parameters: {} },
    ],
    registryVersion: "1.2.1",
    registryStatus: "active",
  },
  {
    name: "notion",
    title: "Notion",
    description: "Read and write Notion pages, databases, and blocks.",
    emoji: "📝",
    installed: true,
    configured: false,
    runTarget: "cloud",
    credentialFields: [
      { key: "NOTION_TOKEN", label: "Notion Integration Token", kind: "password", required: true, placeholder: "secret_...", help: "Create at notion.so/my-integrations" },
    ],
    llmTools: [
      { name: "search_pages", description: "Search Notion pages", parameters: {} },
      { name: "create_page", description: "Create a new page", parameters: {} },
      { name: "append_blocks", description: "Append content blocks", parameters: {} },
    ],
    registryVersion: "1.1.0",
    registryStatus: "active",
  },
  {
    name: "web-search",
    title: "Web Search",
    description: "Search the web and scrape pages for up-to-date information.",
    emoji: "🔍",
    installed: true,
    configured: true,
    runTarget: "cloud",
    credentialFields: [],
    llmTools: [
      { name: "search", description: "Search the web", parameters: {} },
      { name: "scrape_url", description: "Scrape a URL", parameters: {} },
    ],
    registryVersion: "2.0.0",
    registryStatus: "active",
  },
  {
    name: "jira",
    title: "Jira",
    description: "Create and manage Jira issues, sprints, and boards.",
    emoji: "🎯",
    installed: false,
    configured: false,
    runTarget: "cloud",
    credentialFields: [
      { key: "JIRA_URL", label: "Jira URL", kind: "text", required: true, placeholder: "https://your-org.atlassian.net", help: null },
      { key: "JIRA_EMAIL", label: "Email", kind: "text", required: true, placeholder: "you@company.com", help: null },
      { key: "JIRA_API_TOKEN", label: "API Token", kind: "password", required: true, placeholder: null, help: null },
    ],
    llmTools: [
      { name: "create_issue", description: "Create a Jira issue", parameters: {} },
      { name: "search_issues", description: "Search with JQL", parameters: {} },
    ],
    registryVersion: "1.0.3",
    registryStatus: "active",
  },
  {
    name: "postgres",
    title: "PostgreSQL",
    description: "Run read-only SQL queries against PostgreSQL databases.",
    emoji: "🐘",
    installed: false,
    configured: false,
    runTarget: "runner",
    credentialFields: [
      { key: "PG_CONNECTION_STRING", label: "Connection String", kind: "password", required: true, placeholder: "postgresql://...", help: "Read-only credentials recommended" },
    ],
    llmTools: [
      { name: "query", description: "Execute a read-only SQL query", parameters: {} },
      { name: "list_tables", description: "List database tables", parameters: {} },
    ],
    registryVersion: "1.3.0",
    registryStatus: "active",
  },
  {
    name: "email",
    title: "Email (SMTP)",
    description: "Send emails via SMTP. Supports HTML templates.",
    emoji: "📧",
    installed: true,
    configured: true,
    runTarget: "cloud",
    credentialFields: [
      { key: "SMTP_HOST", label: "SMTP Host", kind: "text", required: true, placeholder: "smtp.gmail.com", help: null },
      { key: "SMTP_USER", label: "Username", kind: "text", required: true, placeholder: null, help: null },
      { key: "SMTP_PASS", label: "Password", kind: "password", required: true, placeholder: null, help: null },
    ],
    llmTools: [
      { name: "send_email", description: "Send an email", parameters: {} },
    ],
    registryVersion: "1.0.1",
    registryStatus: "active",
  },
  {
    name: "calendar",
    title: "Google Calendar",
    description: "List, create, and manage Google Calendar events.",
    emoji: "📅",
    installed: false,
    configured: false,
    runTarget: "cloud",
    credentialFields: [
      { key: "GOOGLE_SERVICE_ACCOUNT", label: "Service Account JSON", kind: "textarea", required: true, placeholder: '{"type":"service_account",...}', help: null },
    ],
    llmTools: [
      { name: "list_events", description: "List upcoming events", parameters: {} },
      { name: "create_event", description: "Create a calendar event", parameters: {} },
    ],
    registryVersion: "1.1.2",
    registryStatus: "active",
  },
];

export const MOCK_PROVIDERS: Provider[] = [
  {
    id: "prv_anthropic",
    name: "anthropic",
    displayName: "Anthropic",
    configured: true,
    configuredAt: new Date(Date.now() - 30 * 86400000).toISOString(),
  },
  {
    id: "prv_google",
    name: "google",
    displayName: "Google (Gemini)",
    configured: true,
    configuredAt: new Date(Date.now() - 20 * 86400000).toISOString(),
  },
  {
    id: "prv_openai",
    name: "openai",
    displayName: "OpenAI",
    configured: true,
    configuredAt: new Date(Date.now() - 15 * 86400000).toISOString(),
  },
  {
    id: "prv_xai",
    name: "xai",
    displayName: "xAI (Grok)",
    configured: false,
    configuredAt: null,
  },
];

export const MOCK_RUNNERS: Runner[] = [
  {
    id: "rnr_001",
    name: "on-prem-runner-1",
    status: "online",
    lastHeartbeatAt: new Date(Date.now() - 30000).toISOString(),
    version: "0.9.2",
    createdAt: new Date(Date.now() - 7 * 86400000).toISOString(),
  },
  {
    id: "rnr_002",
    name: "staging-runner",
    status: "offline",
    lastHeartbeatAt: new Date(Date.now() - 3 * 86400000).toISOString(),
    version: "0.9.1",
    createdAt: new Date(Date.now() - 14 * 86400000).toISOString(),
  },
];

export const MOCK_SYSTEM_EVENTS: SystemEvent[] = [
  {
    id: "evt_001",
    severity: "error",
    category: "skill_exec",
    message: "GitHub skill execution failed: rate limit exceeded (403)",
    skillName: "github",
    agentId: "agt_a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    correlationId: "corr_abc123",
    acknowledged: false,
    createdAt: new Date(Date.now() - 5 * 60000).toISOString(),
  },
  {
    id: "evt_002",
    severity: "warning",
    category: "agent",
    message: "Agent 'Data Pipeline Monitor' pod scheduling delayed — insufficient resources",
    skillName: null,
    agentId: "agt_d4e5f6a7-b8c9-0123-defa-234567890123",
    correlationId: null,
    acknowledged: false,
    createdAt: new Date(Date.now() - 15 * 60000).toISOString(),
  },
  {
    id: "evt_003",
    severity: "info",
    category: "agent",
    message: "Agent 'Research Assistant' started successfully",
    skillName: null,
    agentId: "agt_a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    correlationId: null,
    acknowledged: true,
    createdAt: new Date(Date.now() - 2 * 3600000).toISOString(),
  },
  {
    id: "evt_004",
    severity: "error",
    category: "skill_exec",
    message: "Notion skill: invalid integration token",
    skillName: "notion",
    agentId: "agt_b2c3d4e5-f6a7-8901-bcde-f12345678901",
    correlationId: "corr_def456",
    acknowledged: false,
    createdAt: new Date(Date.now() - 4 * 3600000).toISOString(),
  },
  {
    id: "evt_005",
    severity: "info",
    category: "skill_install",
    message: "Skill 'web-search' upgraded to v2.0.0",
    skillName: "web-search",
    agentId: null,
    correlationId: null,
    acknowledged: true,
    createdAt: new Date(Date.now() - 8 * 3600000).toISOString(),
  },
  {
    id: "evt_006",
    severity: "warning",
    category: "billing",
    message: "Credit usage at 78% of monthly budget",
    skillName: null,
    agentId: null,
    correlationId: null,
    acknowledged: false,
    createdAt: new Date(Date.now() - 12 * 3600000).toISOString(),
  },
  {
    id: "evt_007",
    severity: "info",
    category: "agent",
    message: "Agent 'Code Reviewer' completed 47 reviews this session",
    skillName: null,
    agentId: "agt_b2c3d4e5-f6a7-8901-bcde-f12345678901",
    correlationId: null,
    acknowledged: true,
    createdAt: new Date(Date.now() - 24 * 3600000).toISOString(),
  },
];

export const MOCK_APPROVAL_REQUESTS: ApprovalRequest[] = [
  {
    id: "apr_001",
    agentId: "agt_a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    skillName: "github",
    action: "create_pr",
    paramsJson: '{"repo":"HarKro753/EnterpriseAgentOs","title":"fix: resolve auth redirect loop","base":"main"}',
    status: "pending",
    requestedAt: new Date(Date.now() - 10 * 60000).toISOString(),
    decidedAt: null,
    resultJson: null,
  },
  {
    id: "apr_002",
    agentId: "agt_b2c3d4e5-f6a7-8901-bcde-f12345678901",
    skillName: "email",
    action: "send_email",
    paramsJson: '{"to":"team@officeos.co","subject":"Weekly code review summary"}',
    status: "pending",
    requestedAt: new Date(Date.now() - 30 * 60000).toISOString(),
    decidedAt: null,
    resultJson: null,
  },
  {
    id: "apr_003",
    agentId: "agt_c3d4e5f6-a7b8-9012-cdef-123456789012",
    skillName: "linear",
    action: "create_issue",
    paramsJson: '{"title":"Customer reports slow loading on dashboard","priority":"high"}',
    status: "approved",
    requestedAt: new Date(Date.now() - 2 * 3600000).toISOString(),
    decidedAt: new Date(Date.now() - 1.5 * 3600000).toISOString(),
    resultJson: '{"issueId":"LIN-1234"}',
  },
  {
    id: "apr_004",
    agentId: "agt_e5f6a7b8-c9d0-1234-efab-345678901234",
    skillName: "postgres",
    action: "query",
    paramsJson: '{"sql":"DROP TABLE users"}',
    status: "rejected",
    requestedAt: new Date(Date.now() - 6 * 3600000).toISOString(),
    decidedAt: new Date(Date.now() - 5.5 * 3600000).toISOString(),
    resultJson: null,
  },
];

export const MOCK_BILLING = {
  plan: "pro",
  billingCycle: "monthly",
  concurrentAgentLimit: 5,
  creditBudgetPerMonth: 10_000_000,
  creditsUsedThisMonth: 7_800_000,
  creditsRemaining: 2_200_000,
  overageEnabled: false,
  overBudget: false,
  periodStart: new Date(Date.now() - 15 * 86400000).toISOString(),
  periodEnd: new Date(Date.now() + 15 * 86400000).toISOString(),
  isActive: true,
  billingEnabled: true,
};

// Route matching for mock API
type MockRoute = {
  pattern: RegExp;
  method?: string;
  handler: (match: RegExpMatchArray, init?: RequestInit) => unknown;
};

// Mutable state for mock mutations
const mockState = {
  agents: [...MOCK_AGENTS],
  skills: [...MOCK_SKILLS],
  events: [...MOCK_SYSTEM_EVENTS],
  approvals: [...MOCK_APPROVAL_REQUESTS],
};

const MOCK_ROUTES: MockRoute[] = [
  { pattern: /^\/api\/agents$/, method: "GET", handler: () => mockState.agents },
  { pattern: /^\/api\/agents$/, method: "POST", handler: (_m, init) => {
    const body = JSON.parse(init?.body as string);
    const agent: Agent = {
      id: `agt_${crypto.randomUUID()}`,
      name: body.name,
      provider: body.provider,
      model: body.model ?? null,
      status: "pending",
      podName: null,
      serviceUrl: null,
      createdAt: new Date().toISOString(),
    };
    mockState.agents.unshift(agent);
    return agent;
  }},
  { pattern: /^\/api\/agents\/(.+)$/, method: "DELETE", handler: (m) => {
    mockState.agents = mockState.agents.filter(a => a.id !== m[1]);
    return undefined;
  }},
  { pattern: /^\/api\/skills$/, handler: () => mockState.skills },
  { pattern: /^\/api\/skills\/(.+)\/install$/, method: "POST", handler: (m) => {
    const name = decodeURIComponent(m[1]);
    const skill = mockState.skills.find(s => s.name === name);
    if (skill) skill.installed = true;
    return skill;
  }},
  { pattern: /^\/api\/skills\/(.+)\/uninstall$/, method: "POST", handler: (m) => {
    const name = decodeURIComponent(m[1]);
    const skill = mockState.skills.find(s => s.name === name);
    if (skill) { skill.installed = false; skill.configured = false; }
    return skill;
  }},
  { pattern: /^\/api\/skills\/(.+)\/credentials$/, method: "PUT", handler: (m) => {
    const name = decodeURIComponent(m[1]);
    const skill = mockState.skills.find(s => s.name === name);
    if (skill) skill.configured = true;
    return skill;
  }},
  { pattern: /^\/api\/skills\/(.+)\/run-target$/, method: "PUT", handler: (m, init) => {
    const name = decodeURIComponent(m[1]);
    const body = JSON.parse(init?.body as string);
    const skill = mockState.skills.find(s => s.name === name);
    if (skill) skill.runTarget = body.runTarget;
    return skill;
  }},
  { pattern: /^\/api\/providers$/, handler: () => MOCK_PROVIDERS },
  { pattern: /^\/api\/runners$/, handler: () => MOCK_RUNNERS },
  { pattern: /^\/api\/system-events/, handler: () => mockState.events },
  { pattern: /^\/api\/system-events\/(.+)\/acknowledge$/, method: "POST", handler: (m) => {
    const ev = mockState.events.find(e => e.id === m[1]);
    if (ev) ev.acknowledged = true;
    return undefined;
  }},
  { pattern: /^\/api\/approval-requests$/, handler: () => ({ items: mockState.approvals, total: mockState.approvals.length }) },
  { pattern: /^\/api\/approval-requests\/(.+)\/approve$/, method: "POST", handler: (m) => {
    const req = mockState.approvals.find(r => r.id === m[1]);
    if (req) { req.status = "approved"; req.decidedAt = new Date().toISOString(); }
    return undefined;
  }},
  { pattern: /^\/api\/approval-requests\/(.+)\/reject$/, method: "POST", handler: (m) => {
    const req = mockState.approvals.find(r => r.id === m[1]);
    if (req) { req.status = "rejected"; req.decidedAt = new Date().toISOString(); }
    return undefined;
  }},
  { pattern: /^\/api\/skill-registry$/, handler: () => MOCK_SKILLS.filter(s => s.registryVersion).map(s => ({
    id: `reg_${s.name}`,
    name: s.name,
    version: s.registryVersion!,
    npmPackage: `@harro/${s.name}`,
    bundleUrl: null,
    status: s.registryStatus ?? "active",
    publishedById: null,
    createdAt: new Date(Date.now() - 30 * 86400000).toISOString(),
    updatedAt: new Date(Date.now() - 1 * 86400000).toISOString(),
  }))},
  { pattern: /^\/api\/channels\/types$/, handler: () => [
    { type: "slack", displayName: "Slack", description: "Connect a Slack workspace", configFields: [{ key: "SLACK_BOT_TOKEN", label: "Bot Token", kind: "password", required: true, placeholder: "xoxb-...", help: null }] },
    { type: "telegram", displayName: "Telegram", description: "Connect a Telegram bot", configFields: [{ key: "TELEGRAM_BOT_TOKEN", label: "Bot Token", kind: "password", required: true, placeholder: null, help: null }] },
    { type: "discord", displayName: "Discord", description: "Connect a Discord bot", configFields: [{ key: "DISCORD_BOT_TOKEN", label: "Bot Token", kind: "password", required: true, placeholder: null, help: null }] },
  ]},
  { pattern: /^\/api\/channels$/, method: "GET", handler: () => [
    { id: "ch_001", channelType: "slack", displayName: "Production Slack", enabled: true, configured: true },
    { id: "ch_002", channelType: "telegram", displayName: "Alerts Bot", enabled: true, configured: true },
  ]},
  { pattern: /^\/api\/billing\/user\/subscription$/, handler: () => MOCK_BILLING },
  { pattern: /^\/api\/auth\/me$/, handler: () => MOCK_USER },
  { pattern: /^\/api\/custom-skills$/, handler: () => [] },
];

export function mockApiFetch<T>(path: string, init?: RequestInit): T | null {
  const method = (init?.method ?? "GET").toUpperCase();

  for (const route of MOCK_ROUTES) {
    const match = path.match(route.pattern);
    if (!match) continue;
    if (route.method && route.method !== method) continue;
    return route.handler(match, init) as T;
  }

  return null;
}

export function isMockEnabled(): boolean {
  return process.env.NEXT_PUBLIC_MOCK_DATA === "true";
}
