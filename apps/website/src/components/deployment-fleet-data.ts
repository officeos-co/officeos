/* ── Types ────────────────────────────────────────────────── */

export interface BootLog {
	text: string;
	icon?: string; // logo key
}

export interface FleetAgent {
	name: string;
	logs: BootLog[];
	/** Activity logs shown in mini pill after boot — icon + short text */
	activity: BootLog[];
}

/* ── Logo map (subset used in deploy animation) ──────────── */

export const logos: Record<string, string> = {
	Salesforce: "/logos/salesforce.svg",
	Gmail: "/logos/gmail.svg",
	Slack: "/logos/slack.svg",
	Linear: "/logos/linear.svg",
	Notion: "/logos/notion.svg",
	Discord: "/logos/discord.svg",
	HubSpot: "/logos/hubspot.svg",
	GitHub: "/logos/github.svg",
	Jira: "/logos/jira.svg",
	"Google Calendar": "/logos/google-calendar.svg",
	"Google Drive": "/logos/google-drive.svg",
	Browser: "/logos/browser.svg",
	Teams: "/logos/teams.svg",
	Email: "/logos/email.svg",
};

/* ── Starter agents (shown as large boot cards first) ────── */

export const starters: FleetAgent[] = [
	{
		name: "Finance Agent",
		logs: [
			{ text: "pod scheduled" },
			{ text: "image pulled" },
			{ text: "salesforce connected", icon: "Salesforce" },
			{ text: "gmail connected", icon: "Gmail" },
			{ text: "ready", icon: "Slack" },
		],
		activity: [
			{ icon: "Salesforce", text: "Reconciling Q3 invoices" },
			{ icon: "Gmail", text: "Flagged 3 overdue payments" },
			{ icon: "Slack", text: "Notified #finance-ops" },
			{ icon: "Salesforce", text: "Updated 12 records" },
		],
	},
	{
		name: "Support Agent",
		logs: [
			{ text: "pod scheduled" },
			{ text: "image pulled" },
			{ text: "linear connected", icon: "Linear" },
			{ text: "notion connected", icon: "Notion" },
			{ text: "ready", icon: "Discord" },
		],
		activity: [
			{ icon: "Linear", text: "Triaging 8 new tickets" },
			{ icon: "Notion", text: "Drafting KB article" },
			{ icon: "Discord", text: "Replied in #support" },
			{ icon: "Linear", text: "Closed 3 resolved issues" },
		],
	},
	{
		name: "Sales Agent",
		logs: [
			{ text: "pod scheduled" },
			{ text: "image pulled" },
			{ text: "hubspot connected", icon: "HubSpot" },
			{ text: "gmail connected", icon: "Gmail" },
			{ text: "ready", icon: "Slack" },
		],
		activity: [
			{ icon: "HubSpot", text: "Enriching 14 new leads" },
			{ icon: "Gmail", text: "Sent follow-up sequence" },
			{ icon: "Slack", text: "Pinged @sarah re: Acme" },
			{ icon: "HubSpot", text: "Scored 6 leads as Hot" },
		],
	},
];

/* ── Flood agents ────────────────────────────────────────── */

export const floodAgents: FleetAgent[] = [
	{ name: "Ops Agent", logs: [{ text: "scheduled" }, { text: "github", icon: "GitHub" }, { text: "ready", icon: "Jira" }], activity: [{ icon: "GitHub", text: "Merged deploy #412" }, { icon: "Jira", text: "Closed OPS-88" }, { icon: "Slack", text: "Rotated on-call" }] },
	{ name: "Legal Agent", logs: [{ text: "scheduled" }, { text: "notion", icon: "Notion" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "Notion", text: "Reviewing NDA draft" }, { icon: "Gmail", text: "Sent to external counsel" }, { icon: "Slack", text: "Flagged clause 4.2" }] },
	{ name: "Research Agent", logs: [{ text: "scheduled" }, { text: "browser", icon: "Browser" }, { text: "ready", icon: "Notion" }], activity: [{ icon: "Browser", text: "Scanning 23 sources" }, { icon: "Notion", text: "Updated market brief" }, { icon: "Slack", text: "Shared findings" }] },
	{ name: "HR Agent", logs: [{ text: "scheduled" }, { text: "gmail", icon: "Gmail" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "Gmail", text: "Sent offer letter" }, { icon: "Google Calendar", text: "Scheduled onboarding" }, { icon: "Slack", text: "Welcomed new hire" }] },
	{ name: "Marketing Agent", logs: [{ text: "scheduled" }, { text: "hubspot", icon: "HubSpot" }, { text: "ready", icon: "Gmail" }], activity: [{ icon: "HubSpot", text: "Published campaign" }, { icon: "Gmail", text: "Sent newsletter" }, { icon: "Slack", text: "Shared metrics" }] },
	{ name: "Analytics Agent", logs: [{ text: "scheduled" }, { text: "salesforce", icon: "Salesforce" }, { text: "ready", icon: "Notion" }], activity: [{ icon: "Salesforce", text: "Pulled pipeline data" }, { icon: "Notion", text: "Updated dashboard" }, { icon: "Gmail", text: "Sent weekly report" }] },
	{ name: "DevOps Agent", logs: [{ text: "scheduled" }, { text: "github", icon: "GitHub" }, { text: "ready", icon: "Linear" }], activity: [{ icon: "GitHub", text: "Deployed v2.4.1" }, { icon: "Linear", text: "Closed DEV-201" }, { icon: "Slack", text: "Notified #eng" }] },
	{ name: "QA Agent", logs: [{ text: "scheduled" }, { text: "jira", icon: "Jira" }, { text: "ready", icon: "GitHub" }], activity: [{ icon: "Jira", text: "Running test suite" }, { icon: "GitHub", text: "Posted PR review" }, { icon: "Slack", text: "Reported 2 regressions" }] },
	{ name: "Design Agent", logs: [{ text: "scheduled" }, { text: "notion", icon: "Notion" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "Notion", text: "Updated design spec" }, { icon: "Slack", text: "Shared mockups" }, { icon: "Linear", text: "Created DES-44" }] },
	{ name: "Data Agent", logs: [{ text: "scheduled" }, { text: "drive", icon: "Google Drive" }, { text: "ready", icon: "Gmail" }], activity: [{ icon: "Google Drive", text: "Processed 8 CSVs" }, { icon: "Gmail", text: "Sent data summary" }, { icon: "Notion", text: "Updated schema docs" }] },
	{ name: "Security Agent", logs: [{ text: "scheduled" }, { text: "github", icon: "GitHub" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "GitHub", text: "Scanned 14 repos" }, { icon: "Slack", text: "Alerted #security" }, { icon: "Jira", text: "Filed SEC-19" }] },
	{ name: "Billing Agent", logs: [{ text: "scheduled" }, { text: "salesforce", icon: "Salesforce" }, { text: "ready", icon: "Gmail" }], activity: [{ icon: "Salesforce", text: "Generated 30 invoices" }, { icon: "Gmail", text: "Sent payment reminders" }, { icon: "Slack", text: "Flagged 2 overdue" }] },
	{ name: "Onboarding Agent", logs: [{ text: "scheduled" }, { text: "notion", icon: "Notion" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "Notion", text: "Created welcome doc" }, { icon: "Slack", text: "Introduced in #general" }, { icon: "Google Calendar", text: "Booked orientation" }] },
	{ name: "Compliance Agent", logs: [{ text: "scheduled" }, { text: "browser", icon: "Browser" }, { text: "ready", icon: "Notion" }], activity: [{ icon: "Browser", text: "Checking GDPR updates" }, { icon: "Notion", text: "Updated policy log" }, { icon: "Gmail", text: "Alerted legal team" }] },
	{ name: "Logistics Agent", logs: [{ text: "scheduled" }, { text: "jira", icon: "Jira" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "Jira", text: "Tracked 5 shipments" }, { icon: "Slack", text: "Updated #ops" }, { icon: "Gmail", text: "Confirmed delivery" }] },
	{ name: "Inventory Agent", logs: [{ text: "scheduled" }, { text: "salesforce", icon: "Salesforce" }, { text: "ready", icon: "Gmail" }], activity: [{ icon: "Salesforce", text: "Synced stock levels" }, { icon: "Gmail", text: "Reorder alert sent" }, { icon: "Slack", text: "Notified warehouse" }] },
	{ name: "Payroll Agent", logs: [{ text: "scheduled" }, { text: "gmail", icon: "Gmail" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "Gmail", text: "Sent pay stubs" }, { icon: "Salesforce", text: "Updated records" }, { icon: "Slack", text: "Confirmed payroll run" }] },
	{ name: "Recruiting Agent", logs: [{ text: "scheduled" }, { text: "linear", icon: "Linear" }, { text: "ready", icon: "Gmail" }], activity: [{ icon: "Linear", text: "Reviewed 12 applicants" }, { icon: "Gmail", text: "Sent interview invites" }, { icon: "Slack", text: "Updated #hiring" }] },
	{ name: "Customer Agent", logs: [{ text: "scheduled" }, { text: "hubspot", icon: "HubSpot" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "HubSpot", text: "Checked churn signals" }, { icon: "Slack", text: "Pinged CSM team" }, { icon: "Gmail", text: "Sent renewal reminder" }] },
	{ name: "Product Agent", logs: [{ text: "scheduled" }, { text: "linear", icon: "Linear" }, { text: "ready", icon: "Notion" }], activity: [{ icon: "Linear", text: "Prioritized backlog" }, { icon: "Notion", text: "Updated roadmap" }, { icon: "Slack", text: "Shared sprint plan" }] },
	{ name: "Strategy Agent", logs: [{ text: "scheduled" }, { text: "notion", icon: "Notion" }, { text: "ready", icon: "Gmail" }], activity: [{ icon: "Notion", text: "Drafted Q4 OKRs" }, { icon: "Gmail", text: "Shared with exec" }, { icon: "Slack", text: "Scheduled review" }] },
	{ name: "Insights Agent", logs: [{ text: "scheduled" }, { text: "salesforce", icon: "Salesforce" }, { text: "ready", icon: "Notion" }], activity: [{ icon: "Salesforce", text: "Analyzed win rates" }, { icon: "Notion", text: "Published report" }, { icon: "Slack", text: "Shared in #revenue" }] },
	{ name: "Planning Agent", logs: [{ text: "scheduled" }, { text: "calendar", icon: "Google Calendar" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "Google Calendar", text: "Blocked focus time" }, { icon: "Slack", text: "Sent agenda" }, { icon: "Notion", text: "Updated sprint doc" }] },
	{ name: "Reporting Agent", logs: [{ text: "scheduled" }, { text: "drive", icon: "Google Drive" }, { text: "ready", icon: "Gmail" }], activity: [{ icon: "Google Drive", text: "Built exec deck" }, { icon: "Gmail", text: "Sent to stakeholders" }, { icon: "Slack", text: "Posted summary" }] },
	{ name: "Training Agent", logs: [{ text: "scheduled" }, { text: "notion", icon: "Notion" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "Notion", text: "Published new module" }, { icon: "Slack", text: "Announced in #learn" }, { icon: "Gmail", text: "Sent completion certs" }] },
	{ name: "Risk Agent", logs: [{ text: "scheduled" }, { text: "browser", icon: "Browser" }, { text: "ready", icon: "Notion" }], activity: [{ icon: "Browser", text: "Scanned 9 vendors" }, { icon: "Notion", text: "Updated risk matrix" }, { icon: "Slack", text: "Flagged 2 issues" }] },
	{ name: "Procurement Agent", logs: [{ text: "scheduled" }, { text: "salesforce", icon: "Salesforce" }, { text: "ready", icon: "Gmail" }], activity: [{ icon: "Salesforce", text: "Compared 4 quotes" }, { icon: "Gmail", text: "Sent PO to vendor" }, { icon: "Slack", text: "Updated #procurement" }] },
	{ name: "Scheduling Agent", logs: [{ text: "scheduled" }, { text: "calendar", icon: "Google Calendar" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "Google Calendar", text: "Resolved 3 conflicts" }, { icon: "Slack", text: "Confirmed all-hands" }, { icon: "Gmail", text: "Sent calendar links" }] },
	{ name: "Comms Agent", logs: [{ text: "scheduled" }, { text: "slack", icon: "Slack" }, { text: "ready", icon: "Email" }], activity: [{ icon: "Slack", text: "Posted weekly update" }, { icon: "Email", text: "Sent company digest" }, { icon: "Notion", text: "Archived announcements" }] },
	{ name: "Growth Agent", logs: [{ text: "scheduled" }, { text: "hubspot", icon: "HubSpot" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "HubSpot", text: "A/B test results in" }, { icon: "Slack", text: "Shared in #growth" }, { icon: "Gmail", text: "Updated stakeholders" }] },
	{ name: "Infra Agent", logs: [{ text: "scheduled" }, { text: "github", icon: "GitHub" }, { text: "ready", icon: "Linear" }], activity: [{ icon: "GitHub", text: "Scaled to 8 replicas" }, { icon: "Linear", text: "Closed INFRA-72" }, { icon: "Slack", text: "Posted health check" }] },
	{ name: "Platform Agent", logs: [{ text: "scheduled" }, { text: "github", icon: "GitHub" }, { text: "ready", icon: "Jira" }], activity: [{ icon: "GitHub", text: "Merged migration PR" }, { icon: "Jira", text: "Resolved PLAT-55" }, { icon: "Slack", text: "Notified #platform" }] },
	{ name: "API Agent", logs: [{ text: "scheduled" }, { text: "browser", icon: "Browser" }, { text: "ready", icon: "GitHub" }], activity: [{ icon: "Browser", text: "Tested 22 endpoints" }, { icon: "GitHub", text: "Updated API docs" }, { icon: "Slack", text: "All checks passed" }] },
	{ name: "Search Agent", logs: [{ text: "scheduled" }, { text: "browser", icon: "Browser" }, { text: "ready", icon: "Notion" }], activity: [{ icon: "Browser", text: "Re-indexed 4k pages" }, { icon: "Notion", text: "Updated synonyms" }, { icon: "Slack", text: "Search latency -40%" }] },
	{ name: "Content Agent", logs: [{ text: "scheduled" }, { text: "notion", icon: "Notion" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "Notion", text: "Drafted blog post" }, { icon: "Slack", text: "Sent for review" }, { icon: "Gmail", text: "Scheduled publish" }] },
	{ name: "Media Agent", logs: [{ text: "scheduled" }, { text: "drive", icon: "Google Drive" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "Google Drive", text: "Processed 18 assets" }, { icon: "Slack", text: "Shared in #brand" }, { icon: "Notion", text: "Updated asset library" }] },
	{ name: "Events Agent", logs: [{ text: "scheduled" }, { text: "calendar", icon: "Google Calendar" }, { text: "ready", icon: "Gmail" }], activity: [{ icon: "Google Calendar", text: "Created webinar event" }, { icon: "Gmail", text: "Sent 200 invites" }, { icon: "Slack", text: "Posted in #events" }] },
	{ name: "Partners Agent", logs: [{ text: "scheduled" }, { text: "hubspot", icon: "HubSpot" }, { text: "ready", icon: "Gmail" }], activity: [{ icon: "HubSpot", text: "Synced partner deals" }, { icon: "Gmail", text: "Sent co-marketing brief" }, { icon: "Slack", text: "Updated #partners" }] },
	{ name: "Success Agent", logs: [{ text: "scheduled" }, { text: "salesforce", icon: "Salesforce" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "Salesforce", text: "Health scores updated" }, { icon: "Slack", text: "Pinged at-risk accounts" }, { icon: "Gmail", text: "Sent QBR prep" }] },
	{ name: "Enablement Agent", logs: [{ text: "scheduled" }, { text: "notion", icon: "Notion" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "Notion", text: "Published playbook v3" }, { icon: "Slack", text: "Announced in #sales" }, { icon: "Gmail", text: "Sent training links" }] },
	{ name: "Audit Agent", logs: [{ text: "scheduled" }, { text: "browser", icon: "Browser" }, { text: "ready", icon: "Notion" }], activity: [{ icon: "Browser", text: "Checked 11 controls" }, { icon: "Notion", text: "Updated audit trail" }, { icon: "Gmail", text: "Sent compliance report" }] },
	{ name: "Testing Agent", logs: [{ text: "scheduled" }, { text: "github", icon: "GitHub" }, { text: "ready", icon: "Jira" }], activity: [{ icon: "GitHub", text: "Ran 340 tests" }, { icon: "Jira", text: "Filed 2 bugs" }, { icon: "Slack", text: "All suites green" }] },
	{ name: "Workflow Agent", logs: [{ text: "scheduled" }, { text: "linear", icon: "Linear" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "Linear", text: "Automated 5 flows" }, { icon: "Slack", text: "Notified owners" }, { icon: "Notion", text: "Updated runbook" }] },
	{ name: "Forecast Agent", logs: [{ text: "scheduled" }, { text: "salesforce", icon: "Salesforce" }, { text: "ready", icon: "Gmail" }], activity: [{ icon: "Salesforce", text: "Built Q4 forecast" }, { icon: "Gmail", text: "Sent to CFO" }, { icon: "Slack", text: "Posted in #finance" }] },
	{ name: "Integration Agent", logs: [{ text: "scheduled" }, { text: "github", icon: "GitHub" }, { text: "ready", icon: "Notion" }], activity: [{ icon: "GitHub", text: "Synced 3 webhooks" }, { icon: "Notion", text: "Updated API map" }, { icon: "Slack", text: "All integrations live" }] },
];
