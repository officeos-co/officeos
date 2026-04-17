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

export type { AgentLog } from "@/types/logs"

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

export type FileNode = {
  name: string
  type: "file" | "folder"
  children?: FileNode[]
  content?: string
}

// TODO(1.1): wire memory tab to live pod via gateway WebSocket — backend AgentMemory + Vault were removed in 1.0.
export const mockFileTree: FileNode[] = [
  {
    name: "USER.md",
    type: "file",
    content: `# User Context

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
  },
  {
    name: "SOUL.md",
    type: "file",
    content: `# Agent Personality

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
  },
  {
    name: "AGENT.md",
    type: "file",
    content: `# Agent Configuration

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
  },
  {
    name: "memory",
    type: "folder",
    children: [
      {
        name: "user_preferences.md",
        type: "file",
        content: `---
name: user_preferences
type: user
---

User prefers bullet-point summaries over long paragraphs.
Always include source URLs when presenting research.
Timezone is Europe/Berlin — schedule references should use CET/CEST.
`,
      },
      {
        name: "feedback_no_jargon.md",
        type: "file",
        content: `---
name: feedback_no_jargon
type: feedback
---

Avoid technical jargon unless the user uses it first.

**Why:** User explicitly said "keep it simple, I'm not an engineer."
**How to apply:** Default to plain language. Only use technical terms if the user introduced them in the conversation.
`,
      },
      {
        name: "project_q2_report.md",
        type: "file",
        content: `---
name: project_q2_report
type: project
---

Q2 quarterly report is due by 2026-06-30. User is collecting data on AI agent adoption.

**Why:** Management presentation — needs hard numbers with sources.
**How to apply:** When researching AI topics, prioritize recent stats (2025-2026) and save relevant findings for the report.
`,
      },
      {
        name: "reference_notion_workspace.md",
        type: "file",
        content: `---
name: reference_notion_workspace
type: reference
---

Company knowledge base is in Notion workspace "Acme Corp".
Key pages: "Product Roadmap", "Competitive Analysis", "Meeting Notes".
The "Research Archive" database contains past research summaries.
`,
      },
    ],
  },
  {
    name: "vault",
    type: "folder",
    children: [
      {
        name: "IDENTITY.md",
        type: "file",
        content: `# Identity

I am Research Assistant, an AI agent built to help with research and analysis tasks.
I work for Acme Corp and report findings through Slack and Notion.
My primary user is the strategy team.
`,
      },
      {
        name: "AGENTS.md",
        type: "file",
        content: `# Known Agents

## Code Reviewer
- Handles PR reviews on GitHub
- Can be asked to review specific files

## Customer Support Bot
- Handles customer inquiries via Slack
- Escalates technical issues to engineering
`,
      },
      {
        name: "context",
        type: "folder",
        children: [
          {
            name: "company_overview.md",
            type: "file",
            content: `# Acme Corp

B2B SaaS company, 50 employees, Series A.
Products: AgentOS (AI agent platform).
Key markets: Enterprise, mid-market.
Competitors: Relevance AI, CrewAI, AutoGen.
`,
          },
          {
            name: "style_guide.md",
            type: "file",
            content: `# Communication Style Guide

- Use "we" when referring to the company
- Formal but friendly tone
- Always capitalize product names (AgentOS, not agentOS)
- Date format: YYYY-MM-DD
- Currency: EUR unless specified
`,
          },
        ],
      },
    ],
  },
]
