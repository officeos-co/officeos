/**
 * Agent template mock fixtures. The dashboard template picker used to inline
 * this list inside `quickstart/page.tsx`; it now lives here so `useAgentTemplates`
 * can fall back to the same data under USE_MOCKS and so the shape is documented
 * in one place.
 *
 * Backend template payload is a superset — `{ id, name, description, prompt,
 * integrations, channels, isBuiltin }`. The UI only reads the fields in
 * `Template` below, so the hook projects the GraphQL response down to this
 * shape and layout stays identical.
 */

export type Template = {
  name: string
  description: string
  integrations: string[]
  channels: string[]
  prompt: string
}

export const mockTemplates: Template[] = [
  { name: "Blank agent", description: "A blank starting point.", integrations: [], channels: [], prompt: "" },
  { name: "Deep researcher", description: "Multi-step web research with citations.", integrations: ["browser"], channels: [], prompt: "You are a research assistant. Conduct thorough web research, synthesize findings, and present them with source citations." },
  { name: "Support agent", description: "Answers questions from docs, escalates via Slack.", integrations: ["notion"], channels: ["slack"], prompt: "You are a customer support agent. Answer questions using the knowledge base in Notion. Escalate to #support-escalation on Slack when needed." },
  { name: "Incident commander", description: "Triages alerts, creates tickets, runs war room.", integrations: ["linear", "browser"], channels: ["slack"], prompt: "You are an incident commander. Triage alerts, create Linear issues, and coordinate the response in #incidents on Slack." },
  { name: "Code reviewer", description: "Reviews PRs for bugs and security.", integrations: ["github"], channels: [], prompt: "Review pull request diffs for bugs, security vulnerabilities, and style issues. Leave constructive comments." },
  { name: "Feedback miner", description: "Clusters feedback into themes.", integrations: ["notion"], channels: ["slack"], prompt: "Collect feedback from Slack and Notion, cluster into themes, and draft actionable tasks." },
  { name: "Sprint retro", description: "Writes the retro doc from Linear.", integrations: ["linear", "notion"], channels: [], prompt: "Pull completed issues from the latest Linear sprint, identify patterns, and write a retro summary in Notion." },
  { name: "Compliance monitor", description: "Flags regulatory risks.", integrations: ["browser", "notion"], channels: ["slack"], prompt: "Search for regulatory updates, cross-reference against internal policies, and flag risks to #compliance." },
  { name: "Sales assistant", description: "Enriches leads and drafts outreach.", integrations: ["hubspot", "browser"], channels: [], prompt: "Research leads, draft personalized outreach emails, and log activity in HubSpot." },
  { name: "Data analyst", description: "Answers questions from web data.", integrations: ["browser"], channels: [], prompt: "Search for data sources, extract structured information, and present clear answers." },
]
