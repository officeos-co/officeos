import type { AgentLog } from "@/types/logs"

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

export type LogEntry = {
  id: string
  time: number
  model: string
  inputTokens: number
  outputTokens: number
  type: string
  serviceTier: string
  request: string
}

export type DailyUsage = {
  date: string
  inputTokens: number
  outputTokens: number
  requests: number
  rateLimited: number
  webSearches: number
}

export type DailyCost = {
  date: string
  tokenCost: number
  webSearchCost: number
  codeExecCost: number
  runtimeCost: number
}

const models = ["claude-sonnet-4-6", "claude-opus-4-6", "claude-haiku-4-5", "gemini-2.5-pro", "gpt-4o"]
const types = ["chat", "completion", "tool_call", "embedding"]
const tiers = ["standard", "priority", "batch"]
const requests = [
  "Summarize the Q1 report",
  "Create a pull request for auth fix",
  "Search for compliance updates",
  "Draft outreach email for lead",
  "List upcoming calendar events",
  "Analyze customer feedback",
  "Review code diff for security issues",
  "Generate weekly status report",
  "Search Drive for budget spreadsheet",
  "Create Linear issue for bug",
  "Send Slack update to #general",
  "Parse amendment document",
  "Cross-ref internal policies",
  "List open Jira tickets",
  "Update HubSpot contact",
]

function rand(min: number, max: number) {
  return Math.floor(Math.random() * (max - min + 1)) + min
}

function pickRandom<T>(arr: T[]): T {
  return arr[Math.floor(Math.random() * arr.length)]
}

// Generate 50 log entries over the past 7 days
export const mockLogs: LogEntry[] = Array.from({ length: 50 }, (_, i) => ({
  id: `req_${(1000 + i).toString(36)}${rand(100, 999)}`,
  time: Date.now() - rand(0, 7 * 86400000),
  model: pickRandom(models),
  inputTokens: rand(50, 12000),
  outputTokens: rand(20, 8000),
  type: pickRandom(types),
  serviceTier: pickRandom(tiers),
  request: pickRandom(requests),
})).sort((a, b) => b.time - a.time)

// Generate 30 days of usage data
export const mockDailyUsage: DailyUsage[] = Array.from({ length: 30 }, (_, i) => {
  const d = new Date()
  d.setDate(d.getDate() - (29 - i))
  return {
    date: d.toISOString().slice(0, 10),
    inputTokens: rand(5000, 180000),
    outputTokens: rand(2000, 120000),
    requests: rand(10, 250),
    rateLimited: rand(0, 5),
    webSearches: rand(0, 40),
  }
})

// Generate 30 days of cost data
export const mockDailyCost: DailyCost[] = Array.from({ length: 30 }, (_, i) => {
  const d = new Date()
  d.setDate(d.getDate() - (29 - i))
  return {
    date: d.toISOString().slice(0, 10),
    tokenCost: rand(10, 350) / 100,
    webSearchCost: rand(0, 50) / 100,
    codeExecCost: rand(0, 30) / 100,
    runtimeCost: rand(5, 80) / 100,
  }
})
