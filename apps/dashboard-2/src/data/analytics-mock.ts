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
