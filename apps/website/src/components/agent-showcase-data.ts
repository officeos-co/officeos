/* ── Logo map (tools + channels) ──────────────────────────── */

export const logos: Record<string, string> = {
  "Google Calendar": "/logos/google-calendar.svg",
  Notion: "/logos/notion.svg",
  Gmail: "/logos/gmail.svg",
  Slack: "/logos/slack.svg",
  HubSpot: "/logos/hubspot.svg",
  Browser: "/logos/browser.svg",
  WhatsApp: "/logos/whatsapp.svg",
  Telegram: "/logos/telegram.svg",
  Teams: "/logos/teams.svg",
  Discord: "/logos/discord.svg",
  Email: "/logos/email.svg",
  GitHub: "/logos/github.svg",
  Jira: "/logos/jira.svg",
  "Google Drive": "/logos/google-drive.svg",
  Linear: "/logos/linear.svg",
  Salesforce: "/logos/salesforce.svg",
};

/* ── Types ────────────────────────────────────────────────── */

export interface LogEntry {
  time: string;
  text: string;
  icon?: string;
}

export interface ChannelMessage {
  channel: string;
  text: string;
  time: string;
  direction: "sent" | "received";
}

export interface AgentTask {
  name: string;
  done: boolean;
}

export type PhaseStatus = "idle" | "wake" | "boot" | "working";

export interface AgentPhase {
  status: PhaseStatus;
  duration: number;
  statusText: string;
  tasks?: AgentTask[];
  log?: LogEntry[];
  channels?: ChannelMessage[];
  metric?: { label: string; value: string; sub: string };
  lastRunSummary?: string;
  nextRun?: string;
}

export interface Agent {
  id: string;
  name: string;
  role: string;
  tools: string[];
  metric: { label: string; value: string; sub: string }; // fallback
  phases: AgentPhase[];
}

/* ── Agent definitions ────────────────────────────────────── */

export const agents: Agent[] = [
  // ── 1: Already working — user lands here
  {
    id: "a1",
    name: "Briefing Agent",
    role: "Executive Assistant",
    tools: ["Google Calendar", "Notion", "Gmail", "Google Drive", "Slack"],
    metric: { label: "BRIEFINGS SENT", value: "47", sub: "this month" },
    phases: [
      {
        status: "working",
        duration: 7000,
        statusText: "Preparing briefing for Q2 strategy call at 2pm",
        tasks: [
          { name: "Pull calendar context", done: true },
          { name: "Summarize email threads", done: true },
          { name: "Draft briefing doc", done: false },
          { name: "Send to #exec-team", done: false },
        ],
        log: [
          { time: "14:01", text: "Fetched 3 calendar events for today", icon: "Google Calendar" },
          { time: "14:01", text: "Reading 12 email threads from last 48h", icon: "Gmail" },
          { time: "14:02", text: "Identified 4 action items", icon: "Gmail" },
          { time: "14:03", text: "Drafting briefing document…", icon: "Notion" },
          { time: "14:04", text: "Sent briefing to #exec-team", icon: "Slack" },
        ],
        channels: [
          { channel: "Email", text: "Digest sent to 4 recipients", time: "14:02", direction: "sent" },
          { channel: "Slack", text: "Briefing posted to #exec-team", time: "14:04", direction: "sent" },
        ],
      },
    ],
  },

  // ── 2: Receives a message, instantly starts working
  {
    id: "a2",
    name: "Compliance Monitor",
    role: "Legal & Compliance",
    tools: ["Browser", "Notion", "Slack"],
    metric: { label: "RISKS FLAGGED", value: "3", sub: "this week" },
    phases: [
      {
        status: "wake",
        duration: 1800,
        statusText: "Incoming request — activating…",
        metric: { label: "RISKS FLAGGED", value: "3", sub: "this week" },
        channels: [
          { channel: "Slack", text: "@compliance urgent: new GDPR amendment filed", time: "14:06", direction: "received" },
        ],
      },
      {
        status: "working",
        duration: 6000,
        statusText: "Analyzing GDPR amendment impact",
        metric: { label: "RISKS FLAGGED", value: "4", sub: "this week" },
        tasks: [
          { name: "Parse amendment document", done: true },
          { name: "Cross-ref internal policies", done: false },
          { name: "Draft risk assessment", done: false },
          { name: "Alert legal team", done: false },
        ],
        log: [
          { time: "14:06", text: "Received alert from #compliance", icon: "Slack" },
          { time: "14:06", text: "Fetching GDPR amendment text…", icon: "Browser" },
          { time: "14:07", text: "Parsing 12 new clauses", icon: "Browser" },
          { time: "14:07", text: "Cross-referencing internal policies…", icon: "Notion" },
          { time: "14:08", text: "Risk assessment sent to #legal", icon: "Slack" },
        ],
        channels: [
          { channel: "Slack", text: "@compliance urgent: new GDPR amendment filed", time: "14:06", direction: "received" },
          { channel: "Slack", text: "Risk assessment posted to #legal", time: "14:08", direction: "sent" },
          { channel: "Email", text: "GDPR brief sent to legal@company.com", time: "14:08", direction: "sent" },
        ],
      },
    ],
  },

  // ── 3: Brand new agent booting up — appears in sidebar
  {
    id: "a3",
    name: "Pipeline Agent",
    role: "Sales Operations",
    tools: ["HubSpot", "Salesforce", "Browser", "Slack", "Linear", "Gmail"],
    metric: { label: "LEADS ENRICHED", value: "0", sub: "just started" },
    phases: [
      {
        status: "boot",
        duration: 3500,
        statusText: "Initializing…",
        metric: { label: "LEADS ENRICHED", value: "0", sub: "just deployed" },
        log: [
          { time: "14:10", text: "Agent deployed — loading configuration", icon: "Linear" },
          { time: "14:10", text: "Connecting to HubSpot…", icon: "HubSpot" },
          { time: "14:10", text: "Connecting to Salesforce…", icon: "Salesforce" },
          { time: "14:11", text: "All tools ready — starting first task", icon: "Browser" },
        ],
        channels: [],
      },
      {
        status: "working",
        duration: 5500,
        statusText: "Enriching 8 new leads from HubSpot",
        metric: { label: "LEADS ENRICHED", value: "3", sub: "first batch" },
        tasks: [
          { name: "Sync new HubSpot leads", done: true },
          { name: "Enrich company data", done: false },
          { name: "Score & prioritize", done: false },
          { name: "Notify sales reps", done: false },
        ],
        log: [
          { time: "14:11", text: "Pulled 8 new leads from HubSpot", icon: "HubSpot" },
          { time: "14:11", text: "Enriching Acme Corp — found 3 signals", icon: "Browser" },
          { time: "14:12", text: "Scored lead: Acme Corp → Hot", icon: "Salesforce" },
          { time: "14:12", text: "Notified @sarah about Acme Corp", icon: "Slack" },
        ],
        channels: [
          { channel: "Slack", text: "Notified @sarah about Acme Corp", time: "14:12", direction: "sent" },
          { channel: "WhatsApp", text: "Lead alert sent to sales group", time: "14:12", direction: "sent" },
        ],
      },
    ],
  },
];

export const sidebarChannels = [
  { name: "Slack", logo: "/logos/slack.svg" },
  { name: "WhatsApp", logo: "/logos/whatsapp.svg" },
  { name: "Telegram", logo: "/logos/telegram.svg" },
  { name: "Discord", logo: "/logos/discord.svg" },
  { name: "Email", logo: "/logos/email.svg" },
];
