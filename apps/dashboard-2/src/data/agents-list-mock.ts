/**
 * Mock fixtures for the agents list table. Source of truth for the UI shape
 * consumed by `/agents` (list) — the hook maps the GraphQL `agents` query onto
 * this shape. Do not remove: still used as the USE_MOCKS fallback and as the
 * reference for layout fields the backend does not yet provide.
 */

export type AgentListRow = {
  id: string
  name: string
  model: string
  status: string
  created: string
  updated: string
}

export const mockAgentsList: AgentListRow[] = [
  {
    id: "agt_a1b2c3",
    name: "Research Assistant",
    model: "claude-opus-4-6",
    status: "running",
    created: "2 days ago",
    updated: "10 minutes ago",
  },
  {
    id: "agt_b2c3d4",
    name: "Code Reviewer",
    model: "claude-sonnet-4-6",
    status: "running",
    created: "5 days ago",
    updated: "1 hour ago",
  },
  {
    id: "agt_c3d4e5",
    name: "Customer Support Bot",
    model: "gpt-4o",
    status: "running",
    created: "10 days ago",
    updated: "2 hours ago",
  },
  {
    id: "agt_d4e5f6",
    name: "Data Pipeline Monitor",
    model: "claude-haiku-4-5",
    status: "pending",
    created: "1 day ago",
    updated: "1 day ago",
  },
  {
    id: "agt_e5f6a7",
    name: "Content Writer",
    model: "gpt-4o-mini",
    status: "stopped",
    created: "2 weeks ago",
    updated: "7 days ago",
  },
  {
    id: "agt_f6a7b8",
    name: "Security Scanner",
    model: "claude-sonnet-4-6",
    status: "failed",
    created: "3 days ago",
    updated: "30 minutes ago",
  },
]
