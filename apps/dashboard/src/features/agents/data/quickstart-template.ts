export const initialQuickstartYaml = `kind: agent
key: contract_tracker
name: Contract tracker
description: Extracts clauses, sets deadline reminders, and tracks obligations in Asana when given a GitHub issue or link.
model: claude-sonnet-4-6
system: |-
  You are a contract lifecycle assistant. Given a GitHub issue or link:

  1. Read the file and extract key metadata: parties, effective date, expiration date, contract value, type, and obligations.
  2. Create an Asana list named "<Counterparty> - <Contract Type> - <Effective Year>" with custom fields for counterparty, contract value, and type.
  3. For each critical date (renewals, expirations, payment due dates, notice periods), create an Asana task titled "[CONTRACT DATE] <Event> - <Contract Name>" with the source clause, due date, and priority.
  4. For each obligation or SLA, create an Asana task assigned to the relevant team member, tagged by category (Payment, Delivery, Compliance, Renewal, SLA), with the verbatim contract clause as a comment.

  Rules: always quote the original clause text - never paraphrase without it. If a date or clause is ambiguous, flag it rather than assume.
mcp_servers:
  - name: github
    type: registered
  - name: asana
    type: registered
tools:
  - type: agent_toolset_20260401
  - type: mcp_toolset
    mcp_server_name: github
    default_config:
      permission_policy:
        type: always_allow
  - type: mcp_toolset
    mcp_server_name: asana
    default_config:
      permission_policy:
        type: always_allow
metadata:
  template: contract-clause-extraction`;

export const initialQuickstartFiles = [
  {
    path: "workspace.yaml",
    content: `kind: workspace
resources:
  browsers:
    - key: contract_browser
      display_name: Contract Browser
  memory_stores:
    - key: contract_memory
      display_name: Contract Memory
agents:
  - key: contract_tracker
    file: agents/contract-tracker.yaml`,
  },
  {
    path: "agents/contract-tracker.yaml",
    content: `${initialQuickstartYaml}
resources:
  - type: browser
    ref: contract_browser
    access_mode: read_write
    instructions: Use this browser to inspect web pages and verify external workflows.
  - type: memory_store
    ref: contract_memory
    access_mode: read_write
    instructions: Store extracted contract obligations, dates, and follow-up decisions.`,
  },
];

export const initialQuickstartMessages: Array<{
  id: string;
  role: "agent" | "user";
  content: string;
}> = [
  {
    id: "agent-intro",
    role: "agent",
    content:
      "Tell me what kind of agent you want to build. I will generate the declarative template on the right while you refine the prompt.",
  },
];

export function buildMockQuickstartYaml(prompt: string) {
  const normalizedName =
    prompt
      .trim()
      .replace(/[^a-zA-Z0-9 ]/g, "")
      .split(/\s+/)
      .slice(0, 4)
      .join(" ") || "Operations assistant";

  return `name: ${normalizedName}
description: Draft agent generated from the latest quickstart prompt.
model: claude-sonnet-4-6
system: |-
  You are an operator assistant for EnterpriseAgentOs.

  User request:
    ${prompt.trim() || "Create a reliable workflow agent from a short description."}

  Responsibilities:
    1. Clarify missing requirements before taking irreversible action.
    2. Use connected MCP servers for external systems.
    3. Keep a structured audit trail of decisions, tool calls, and outcomes.
    4. Return concise status updates with next actions.

  Rules:
    - Prefer explicit user confirmation for destructive changes.
    - Quote source records when summarizing external data.
    - Flag ambiguous inputs instead of inventing details.
mcp_servers:
  - name: workspace
    type: url
    url: https://mcp.enterprise.local/workspace
  - name: notifications
    type: url
    url: https://mcp.enterprise.local/notifications/sse
tools:
  - type: agent_toolset_20260401
  - type: mcp_toolset
    mcp_server_name: workspace
    default_config:
      permission_policy:
        type: always_allow
  - type: mcp_toolset
    mcp_server_name: notifications
    default_config:
      permission_policy:
        type: always_allow
metadata:
  template: quickstart`;
}
