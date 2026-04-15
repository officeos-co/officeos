export type AgentDetail = {
  id: string
  name: string
  model: string
  status: string
  prompt: string
  integrations: string[]
  channels: string[]
  createdAt: number
}

export type AgentLog = {
  id: string
  time: number
  type: "tool_call" | "tool_result" | "message_in" | "message_out" | "channel_in" | "channel_out" | "system"
  tool?: string
  integration?: string
  channel?: string
  content: string
  durationMs?: number
  tokens?: { input: number; output: number }
}

export const mockAgent: AgentDetail = {
  id: "agt_a1b2c3",
  name: "Research Assistant",
  model: "claude-sonnet-4-6",
  status: "running",
  prompt: "You are a research assistant. When given a topic, conduct thorough web research, synthesize findings, and present them with source citations. Always verify claims across multiple sources.",
  integrations: ["github", "browser", "notion"],
  channels: ["slack"],
  createdAt: Date.now() - 2 * 86400000,
}

export const mockAgentLogs: AgentLog[] = [
  { id: "log_001", time: Date.now() - 120000, type: "channel_in", channel: "slack", content: "@research-bot Can you find the latest data on AI agent adoption in enterprise?", },
  { id: "log_002", time: Date.now() - 115000, type: "tool_call", tool: "search", integration: "browser", content: "search --query \"AI agent adoption enterprise 2026 statistics\"", durationMs: 1240, },
  { id: "log_003", time: Date.now() - 113000, type: "tool_result", tool: "search", integration: "browser", content: "Found 8 results. Top: Gartner reports 35% of enterprises now use AI agents...", },
  { id: "log_004", time: Date.now() - 110000, type: "tool_call", tool: "search", integration: "browser", content: "scrape_url --url \"https://gartner.com/ai-agents-2026\"", durationMs: 2100, },
  { id: "log_005", time: Date.now() - 107000, type: "tool_result", tool: "scrape_url", integration: "browser", content: "Scraped 4,200 words. Key finding: 35% adoption rate, up from 12% in 2024...", },
  { id: "log_006", time: Date.now() - 100000, type: "tool_call", tool: "search", integration: "notion", content: "search --query \"AI adoption internal research\"", durationMs: 890, },
  { id: "log_007", time: Date.now() - 98000, type: "tool_result", tool: "search", integration: "notion", content: "Found 2 pages: \"Q1 AI Strategy Review\", \"Competitive Analysis - AI Agents\"", },
  { id: "log_008", time: Date.now() - 90000, type: "channel_out", channel: "slack", content: "Here's what I found on AI agent adoption in enterprise:\n\n**Key statistics (2026):**\n- 35% of enterprises now deploy AI agents (Gartner)\n- Up from 12% in 2024\n- Average ROI: 3.2x in the first year\n\nI also found 2 relevant internal documents in Notion that may be useful.", },
  { id: "log_009", time: Date.now() - 60000, type: "channel_in", channel: "slack", content: "Great, can you also create a GitHub issue to track adding this to our quarterly report?", },
  { id: "log_010", time: Date.now() - 55000, type: "tool_call", tool: "create_issue", integration: "github", content: "create_issue --repo \"acme/quarterly-reports\" --title \"Add AI agent adoption data to Q2 report\" --labels \"research,data\"", durationMs: 1560, },
  { id: "log_011", time: Date.now() - 52000, type: "tool_result", tool: "create_issue", integration: "github", content: "Created issue #142: \"Add AI agent adoption data to Q2 report\"", },
  { id: "log_012", time: Date.now() - 50000, type: "channel_out", channel: "slack", content: "Done! Created GitHub issue #142 in acme/quarterly-reports to track adding the AI adoption data to the Q2 report.", },
  { id: "log_013", time: Date.now() - 30000, type: "system", content: "Agent heartbeat OK. Memory: 128MB, CPU: 2%", },
  { id: "log_014", time: Date.now() - 10000, type: "system", content: "Agent idle. Waiting for input.", },
]

export const mockMemoryFiles: Record<string, string> = {
  "USER.md": `# User Context

## Preferences
- Prefers concise summaries with bullet points
- Wants source links included in research outputs
- Timezone: Europe/Berlin
- Language: English

## Past Interactions
- Frequently asks about AI/ML industry trends
- Interested in competitive analysis
- Works on quarterly reports for management
`,
  "SOUL.md": `# Agent Personality

## Core Traits
- Thorough and methodical in research
- Always cites sources
- Proactively suggests related topics
- Asks clarifying questions when the request is ambiguous

## Communication Style
- Professional but approachable
- Uses markdown formatting for readability
- Keeps responses focused and actionable
- Avoids jargon unless the user uses it first

## Boundaries
- Never fabricates data or sources
- Always indicates confidence level
- Escalates to human when outside expertise
`,
  "AGENT.md": `# Agent Configuration

## Identity
- Name: Research Assistant
- Role: Research and analysis support
- Created: 2026-04-14

## Capabilities
- Web research and synthesis
- Internal knowledge base search (Notion)
- GitHub issue management
- Slack communication

## Operating Rules
- Maximum 5 tool calls per request before checking in with user
- Always verify facts across 2+ sources
- Log all research sources for audit trail
- Respond within 30 seconds of receiving a message
`,
}
