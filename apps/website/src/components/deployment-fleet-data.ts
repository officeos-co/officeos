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
	Confluence: "/logos/confluence.svg",
	Airtable: "/logos/airtable.svg",
	Intercom: "/logos/intercom.svg",
	Zendesk: "/logos/zendesk.svg",
	Figma: "/logos/figma.svg",
	"Google Calendar": "/logos/google-calendar.svg",
	"Google Drive": "/logos/google-drive.svg",
	"Google Analytics": "/logos/google-analytics.svg",
	Browser: "/logos/chrome.svg",
	Teams: "/logos/teams.svg",
	Email: "/logos/email.svg",
	AWS: "/logos/aws.svg",
	Azure: "/logos/azure.svg",
	Terraform: "/logos/terraform.svg",
	Snowflake: "/logos/snowflake.svg",
	Tableau: "/logos/tableau.svg",
	"Power BI": "/logos/powerbi.svg",
	PostgreSQL: "/logos/postgresql.svg",
	MongoDB: "/logos/mongodb.svg",
	Redis: "/logos/redis.svg",
	"Google Cloud": "/logos/google-cloud.svg",
	Datadog: "/logos/datadog.svg",
	Sentry: "/logos/sentry.svg",
	PagerDuty: "/logos/pagerduty.svg",
	Supabase: "/logos/supabase.svg",
	Zapier: "/logos/zapier.svg",
	Stripe: "/logos/stripe.svg",
};

/* ── Starter agents (shown as large boot cards first) ────── */

export const starters: FleetAgent[] = [
	{
		name: "Sales Agent",
		logs: [
			{ text: "workflow queued" },
			{ text: "crm connected", icon: "Salesforce" },
			{ text: "inbox connected", icon: "Gmail" },
			{ text: "brief loaded", icon: "Notion" },
			{ text: "ready", icon: "Slack" },
		],
		activity: [
			{ icon: "Salesforce", text: "Updated 18 opportunities" },
			{ icon: "Gmail", text: "Drafted follow-ups" },
			{ icon: "Notion", text: "Added account notes" },
			{ icon: "Slack", text: "Briefed sales channel" },
		],
	},
	{
		name: "Data Agent",
		logs: [
			{ text: "workflow queued" },
			{ text: "snowflake connected", icon: "Snowflake" },
			{ text: "postgresql ready", icon: "PostgreSQL" },
			{ text: "dashboard synced", icon: "Tableau" },
			{ text: "ready", icon: "Slack" },
		],
		activity: [
			{ icon: "Snowflake", text: "Analyzed revenue trends" },
			{ icon: "PostgreSQL", text: "Checked data freshness" },
			{ icon: "Tableau", text: "Refreshed executive dashboard" },
			{ icon: "Slack", text: "Posted weekly insight" },
		],
	},
	{
		name: "Support Agent",
		logs: [
			{ text: "workflow queued" },
			{ text: "zendesk connected", icon: "Zendesk" },
			{ text: "knowledge loaded", icon: "Confluence" },
			{ text: "alerts connected", icon: "PagerDuty" },
			{ text: "ready", icon: "Teams" },
		],
		activity: [
			{ icon: "Zendesk", text: "Triaged 24 tickets" },
			{ icon: "Confluence", text: "Linked answer sources" },
			{ icon: "PagerDuty", text: "Escalated urgent case" },
			{ icon: "Teams", text: "Notified support lead" },
		],
	},
];

/* ── Flood agents ────────────────────────────────────────── */

export const floodAgents: FleetAgent[] = [
	{ name: "Lead Agent", logs: [{ text: "scheduled" }, { text: "salesforce", icon: "Salesforce" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "Salesforce", text: "Qualified 32 leads" }, { icon: "Slack", text: "Shared hot accounts" }, { icon: "Gmail", text: "Drafted outreach" }] },
	{ name: "Inbox Agent", logs: [{ text: "scheduled" }, { text: "gmail", icon: "Gmail" }, { text: "ready", icon: "Notion" }], activity: [{ icon: "Gmail", text: "Sorted priority mail" }, { icon: "Notion", text: "Captured follow-ups" }, { icon: "Slack", text: "Escalated approvals" }] },
	{ name: "Support Agent", logs: [{ text: "scheduled" }, { text: "zendesk", icon: "Zendesk" }, { text: "ready", icon: "Confluence" }], activity: [{ icon: "Zendesk", text: "Tagged 18 tickets" }, { icon: "Confluence", text: "Found answer sources" }, { icon: "Teams", text: "Updated support queue" }] },
	{ name: "CRM Agent", logs: [{ text: "scheduled" }, { text: "hubspot", icon: "HubSpot" }, { text: "ready", icon: "Salesforce" }], activity: [{ icon: "HubSpot", text: "Enriched 44 contacts" }, { icon: "Salesforce", text: "Synced deal stages" }, { icon: "Gmail", text: "Queued reminders" }] },
	{ name: "Report Agent", logs: [{ text: "scheduled" }, { text: "snowflake", icon: "Snowflake" }, { text: "ready", icon: "Tableau" }], activity: [{ icon: "Snowflake", text: "Checked revenue data" }, { icon: "Tableau", text: "Refreshed board pack" }, { icon: "Slack", text: "Posted summary" }] },
	{ name: "Finance Agent", logs: [{ text: "scheduled" }, { text: "stripe", icon: "Stripe" }, { text: "ready", icon: "Google Drive" }], activity: [{ icon: "Stripe", text: "Matched 30 invoices" }, { icon: "Google Drive", text: "Filed receipts" }, { icon: "Snowflake", text: "Updated MRR table" }] },
	{ name: "Calendar Agent", logs: [{ text: "scheduled" }, { text: "calendar", icon: "Google Calendar" }, { text: "ready", icon: "Teams" }], activity: [{ icon: "Google Calendar", text: "Resolved meeting conflicts" }, { icon: "Teams", text: "Sent agenda notes" }, { icon: "Notion", text: "Prepared brief" }] },
	{ name: "Docs Agent", logs: [{ text: "scheduled" }, { text: "notion", icon: "Notion" }, { text: "ready", icon: "Google Drive" }], activity: [{ icon: "Notion", text: "Updated project spec" }, { icon: "Google Drive", text: "Indexed shared docs" }, { icon: "Slack", text: "Requested review" }] },
	{ name: "Roadmap Agent", logs: [{ text: "scheduled" }, { text: "linear", icon: "Linear" }, { text: "ready", icon: "GitHub" }], activity: [{ icon: "Linear", text: "Prioritized backlog" }, { icon: "GitHub", text: "Linked shipped work" }, { icon: "Slack", text: "Posted release notes" }] },
	{ name: "Design Agent", logs: [{ text: "scheduled" }, { text: "figma", icon: "Figma" }, { text: "ready", icon: "Notion" }], activity: [{ icon: "Figma", text: "Collected design changes" }, { icon: "Notion", text: "Updated handoff notes" }, { icon: "Linear", text: "Created review tasks" }] },
	{ name: "Ops Agent", logs: [{ text: "scheduled" }, { text: "datadog", icon: "Datadog" }, { text: "ready", icon: "PagerDuty" }], activity: [{ icon: "Datadog", text: "Checked service dashboard" }, { icon: "PagerDuty", text: "Closed stale alert" }, { icon: "Sentry", text: "Grouped new issue" }] },
	{ name: "Security Agent", logs: [{ text: "scheduled" }, { text: "github", icon: "GitHub" }, { text: "ready", icon: "Sentry" }], activity: [{ icon: "GitHub", text: "Reviewed security PR" }, { icon: "Sentry", text: "Verified issue owner" }, { icon: "PagerDuty", text: "Updated escalation" }] },
	{ name: "People Agent", logs: [{ text: "scheduled" }, { text: "notion", icon: "Notion" }, { text: "ready", icon: "Google Drive" }], activity: [{ icon: "Notion", text: "Prepared onboarding plan" }, { icon: "Google Drive", text: "Filed signed forms" }, { icon: "Slack", text: "Welcomed new hire" }] },
	{ name: "Marketing Agent", logs: [{ text: "scheduled" }, { text: "google analytics", icon: "Google Analytics" }, { text: "ready", icon: "HubSpot" }], activity: [{ icon: "HubSpot", text: "Segmented campaign list" }, { icon: "Google Analytics", text: "Pulled traffic metrics" }, { icon: "Slack", text: "Shared weekly wins" }] },
	{ name: "Procure Agent", logs: [{ text: "scheduled" }, { text: "jira", icon: "Jira" }, { text: "ready", icon: "Airtable" }], activity: [{ icon: "Jira", text: "Collected purchase requests" }, { icon: "Airtable", text: "Updated vendor status" }, { icon: "Gmail", text: "Drafted approvals" }] },
	{ name: "Legal Agent", logs: [{ text: "scheduled" }, { text: "google drive", icon: "Google Drive" }, { text: "ready", icon: "Notion" }], activity: [{ icon: "Google Drive", text: "Found contract clauses" }, { icon: "Notion", text: "Updated review notes" }, { icon: "Slack", text: "Flagged approval needed" }] },
	{ name: "Research Agent", logs: [{ text: "scheduled" }, { text: "browser", icon: "Browser" }, { text: "ready", icon: "Notion" }], activity: [{ icon: "Browser", text: "Compared 12 competitors" }, { icon: "Notion", text: "Wrote research brief" }, { icon: "Slack", text: "Sent market update" }] },
	{ name: "Sync Agent", logs: [{ text: "scheduled" }, { text: "zapier", icon: "Zapier" }, { text: "ready", icon: "Supabase" }], activity: [{ icon: "Zapier", text: "Triggered 14 workflows" }, { icon: "Supabase", text: "Synced customer records" }, { icon: "PostgreSQL", text: "Checked record counts" }] },
	{ name: "Analytics Agent", logs: [{ text: "scheduled" }, { text: "mongodb", icon: "MongoDB" }, { text: "ready", icon: "Power BI" }], activity: [{ icon: "MongoDB", text: "Sampled product events" }, { icon: "Power BI", text: "Updated KPI deck" }, { icon: "Slack", text: "Shared anomaly note" }] },
	{ name: "Meeting Agent", logs: [{ text: "scheduled" }, { text: "google meet", icon: "Google Calendar" }, { text: "ready", icon: "Notion" }], activity: [{ icon: "Google Calendar", text: "Captured action items" }, { icon: "Notion", text: "Published meeting notes" }, { icon: "Linear", text: "Created follow-up tasks" }] },
	{ name: "Customer Agent", logs: [{ text: "scheduled" }, { text: "intercom", icon: "Intercom" }, { text: "ready", icon: "HubSpot" }], activity: [{ icon: "Intercom", text: "Summarized account health" }, { icon: "HubSpot", text: "Updated lifecycle stage" }, { icon: "Slack", text: "Flagged churn risk" }] },
	{ name: "QA Agent", logs: [{ text: "scheduled" }, { text: "github", icon: "GitHub" }, { text: "ready", icon: "Sentry" }], activity: [{ icon: "GitHub", text: "Checked release checklist" }, { icon: "Sentry", text: "Verified error trend" }, { icon: "Linear", text: "Updated test tasks" }] },
	{ name: "Vendor Agent", logs: [{ text: "scheduled" }, { text: "airtable", icon: "Airtable" }, { text: "ready", icon: "Gmail" }], activity: [{ icon: "Airtable", text: "Reviewed vendor renewals" }, { icon: "Gmail", text: "Drafted renewal emails" }, { icon: "Slack", text: "Requested budget approval" }] },
	{ name: "Knowledge Agent", logs: [{ text: "scheduled" }, { text: "confluence", icon: "Confluence" }, { text: "ready", icon: "Notion" }], activity: [{ icon: "Confluence", text: "Found stale articles" }, { icon: "Notion", text: "Merged duplicate notes" }, { icon: "Slack", text: "Asked owners to review" }] },
	{ name: "Revenue Agent", logs: [{ text: "scheduled" }, { text: "stripe", icon: "Stripe" }, { text: "ready", icon: "Salesforce" }], activity: [{ icon: "Stripe", text: "Checked renewal payments" }, { icon: "Salesforce", text: "Updated forecast notes" }, { icon: "Snowflake", text: "Verified ARR rollup" }] },
	{ name: "Launch Agent", logs: [{ text: "scheduled" }, { text: "linear", icon: "Linear" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "Linear", text: "Confirmed launch tasks" }, { icon: "Slack", text: "Posted launch checklist" }, { icon: "Notion", text: "Updated release page" }] },
	{ name: "Compliance Agent", logs: [{ text: "scheduled" }, { text: "google drive", icon: "Google Drive" }, { text: "ready", icon: "Airtable" }], activity: [{ icon: "Google Drive", text: "Collected audit evidence" }, { icon: "Airtable", text: "Updated control owner" }, { icon: "Gmail", text: "Requested missing proof" }] },
	{ name: "Forecast Agent", logs: [{ text: "scheduled" }, { text: "snowflake", icon: "Snowflake" }, { text: "ready", icon: "Salesforce" }], activity: [{ icon: "Snowflake", text: "Refreshed pipeline model" }, { icon: "Salesforce", text: "Flagged forecast gaps" }, { icon: "Slack", text: "Briefed leadership" }] },
	{ name: "Partner Agent", logs: [{ text: "scheduled" }, { text: "hubspot", icon: "HubSpot" }, { text: "ready", icon: "Google Drive" }], activity: [{ icon: "HubSpot", text: "Updated partner records" }, { icon: "Google Drive", text: "Shared enablement docs" }, { icon: "Gmail", text: "Drafted co-sell note" }] },
	{ name: "Feedback Agent", logs: [{ text: "scheduled" }, { text: "intercom", icon: "Intercom" }, { text: "ready", icon: "Linear" }], activity: [{ icon: "Intercom", text: "Clustered feature asks" }, { icon: "Linear", text: "Linked customer signals" }, { icon: "Notion", text: "Updated voice-of-customer page" }] },
	{ name: "Expense Agent", logs: [{ text: "scheduled" }, { text: "stripe", icon: "Stripe" }, { text: "ready", icon: "Google Drive" }], activity: [{ icon: "Stripe", text: "Matched subscription charges" }, { icon: "Google Drive", text: "Filed finance exports" }, { icon: "Slack", text: "Flagged unusual spend" }] },
	{ name: "Training Agent", logs: [{ text: "scheduled" }, { text: "confluence", icon: "Confluence" }, { text: "ready", icon: "Teams" }], activity: [{ icon: "Confluence", text: "Built lesson outline" }, { icon: "Teams", text: "Scheduled enablement session" }, { icon: "Notion", text: "Updated curriculum" }] },
	{ name: "Incident Agent", logs: [{ text: "scheduled" }, { text: "pagerduty", icon: "PagerDuty" }, { text: "ready", icon: "Datadog" }], activity: [{ icon: "PagerDuty", text: "Summarized incident" }, { icon: "Datadog", text: "Linked dashboard context" }, { icon: "Slack", text: "Posted status update" }] },
	{ name: "Content Agent", logs: [{ text: "scheduled" }, { text: "notion", icon: "Notion" }, { text: "ready", icon: "Google Drive" }], activity: [{ icon: "Notion", text: "Drafted newsletter" }, { icon: "Google Drive", text: "Attached approved assets" }, { icon: "HubSpot", text: "Queued campaign copy" }] },
	{ name: "Account Agent", logs: [{ text: "scheduled" }, { text: "salesforce", icon: "Salesforce" }, { text: "ready", icon: "Gmail" }], activity: [{ icon: "Salesforce", text: "Prepared QBR notes" }, { icon: "Gmail", text: "Drafted exec email" }, { icon: "Notion", text: "Saved account plan" }] },
	{ name: "Survey Agent", logs: [{ text: "scheduled" }, { text: "airtable", icon: "Airtable" }, { text: "ready", icon: "Slack" }], activity: [{ icon: "Airtable", text: "Grouped survey results" }, { icon: "Slack", text: "Shared top themes" }, { icon: "Linear", text: "Created product inputs" }] },
	{ name: "Review Agent", logs: [{ text: "scheduled" }, { text: "github", icon: "GitHub" }, { text: "ready", icon: "Linear" }], activity: [{ icon: "GitHub", text: "Summarized open PRs" }, { icon: "Linear", text: "Updated review queue" }, { icon: "Slack", text: "Pinged reviewers" }] },
	{ name: "Renewal Agent", logs: [{ text: "scheduled" }, { text: "hubspot", icon: "HubSpot" }, { text: "ready", icon: "Stripe" }], activity: [{ icon: "HubSpot", text: "Found renewal risks" }, { icon: "Stripe", text: "Checked billing status" }, { icon: "Gmail", text: "Drafted save plan" }] },
	{ name: "Planning Agent", logs: [{ text: "scheduled" }, { text: "jira", icon: "Jira" }, { text: "ready", icon: "Notion" }], activity: [{ icon: "Jira", text: "Grouped sprint work" }, { icon: "Notion", text: "Updated planning doc" }, { icon: "Slack", text: "Shared capacity note" }] },
	{ name: "Pipeline Agent", logs: [{ text: "scheduled" }, { text: "salesforce", icon: "Salesforce" }, { text: "ready", icon: "Tableau" }], activity: [{ icon: "Salesforce", text: "Scored pipeline health" }, { icon: "Tableau", text: "Updated funnel chart" }, { icon: "Slack", text: "Briefed revenue team" }] },
	{ name: "Pulse Agent", logs: [{ text: "scheduled" }, { text: "datadog", icon: "Datadog" }, { text: "ready", icon: "Sentry" }], activity: [{ icon: "Datadog", text: "Checked service pulse" }, { icon: "Sentry", text: "Summarized issue trend" }, { icon: "PagerDuty", text: "Confirmed coverage" }] },
];
