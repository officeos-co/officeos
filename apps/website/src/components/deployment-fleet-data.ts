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
	Browser: "/logos/chrome.svg",
	Teams: "/logos/teams.svg",
	Email: "/logos/email.svg",
	AWS: "/logos/aws.svg",
	Azure: "/logos/azure.svg",
	Docker: "/logos/docker.svg",
	Kubernetes: "/logos/kubernetes.svg",
	Terraform: "/logos/terraform.svg",
	Snowflake: "/logos/snowflake.svg",
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
		name: "Infra Agent",
		logs: [
			{ text: "pod scheduled" },
			{ text: "image pulled" },
			{ text: "aws connected", icon: "AWS" },
			{ text: "terraform loaded", icon: "Terraform" },
			{ text: "ready", icon: "Kubernetes" },
		],
		activity: [
			{ icon: "AWS", text: "Scaling us-east-1 cluster" },
			{ icon: "Terraform", text: "Applied 3 plan changes" },
			{ icon: "Datadog", text: "All monitors green" },
			{ icon: "Kubernetes", text: "Rolled out v2.8.1" },
		],
	},
	{
		name: "Data Agent",
		logs: [
			{ text: "pod scheduled" },
			{ text: "image pulled" },
			{ text: "snowflake connected", icon: "Snowflake" },
			{ text: "postgresql ready", icon: "PostgreSQL" },
			{ text: "ready", icon: "Supabase" },
		],
		activity: [
			{ icon: "Snowflake", text: "Running ETL pipeline" },
			{ icon: "PostgreSQL", text: "Migrated 4 tables" },
			{ icon: "Redis", text: "Cache hit rate 98.2%" },
			{ icon: "Supabase", text: "Synced edge functions" },
		],
	},
	{
		name: "Platform Agent",
		logs: [
			{ text: "pod scheduled" },
			{ text: "image pulled" },
			{ text: "azure connected", icon: "Azure" },
			{ text: "docker registry ok", icon: "Docker" },
			{ text: "ready", icon: "Google Cloud" },
		],
		activity: [
			{ icon: "Azure", text: "Provisioned 3 VMs" },
			{ icon: "Docker", text: "Built 8 images" },
			{ icon: "Google Cloud", text: "CDN cache purged" },
			{ icon: "PagerDuty", text: "On-call rotated" },
		],
	},
];

/* ── Flood agents ────────────────────────────────────────── */

export const floodAgents: FleetAgent[] = [
	{ name: "Deploy Agent", logs: [{ text: "scheduled" }, { text: "docker", icon: "Docker" }, { text: "ready", icon: "Kubernetes" }], activity: [{ icon: "Docker", text: "Built 12 images" }, { icon: "Kubernetes", text: "Rolled out v3.1" }, { icon: "Datadog", text: "All pods healthy" }] },
	{ name: "Cloud Agent", logs: [{ text: "scheduled" }, { text: "aws", icon: "AWS" }, { text: "ready", icon: "Terraform" }], activity: [{ icon: "AWS", text: "Resized RDS instance" }, { icon: "Terraform", text: "Applied 2 changes" }, { icon: "Datadog", text: "Latency p99 < 50ms" }] },
	{ name: "Monitor Agent", logs: [{ text: "scheduled" }, { text: "datadog", icon: "Datadog" }, { text: "ready", icon: "PagerDuty" }], activity: [{ icon: "Datadog", text: "28 monitors green" }, { icon: "PagerDuty", text: "Rotated on-call" }, { icon: "Sentry", text: "0 new errors" }] },
	{ name: "DB Agent", logs: [{ text: "scheduled" }, { text: "postgresql", icon: "PostgreSQL" }, { text: "ready", icon: "Redis" }], activity: [{ icon: "PostgreSQL", text: "Vacuumed 14 tables" }, { icon: "Redis", text: "Eviction rate 0.1%" }, { icon: "MongoDB", text: "Compacted shards" }] },
	{ name: "ETL Agent", logs: [{ text: "scheduled" }, { text: "snowflake", icon: "Snowflake" }, { text: "ready", icon: "PostgreSQL" }], activity: [{ icon: "Snowflake", text: "Loaded 2.1M rows" }, { icon: "PostgreSQL", text: "Schema migrated" }, { icon: "Supabase", text: "Edge fn updated" }] },
	{ name: "CI Agent", logs: [{ text: "scheduled" }, { text: "github", icon: "GitHub" }, { text: "ready", icon: "Docker" }], activity: [{ icon: "GitHub", text: "Merged 5 PRs" }, { icon: "Docker", text: "Pushed 3 images" }, { icon: "Sentry", text: "Release tagged" }] },
	{ name: "Scale Agent", logs: [{ text: "scheduled" }, { text: "kubernetes", icon: "Kubernetes" }, { text: "ready", icon: "AWS" }], activity: [{ icon: "Kubernetes", text: "HPA scaled to 12" }, { icon: "AWS", text: "Spot instances added" }, { icon: "Datadog", text: "CPU usage 45%" }] },
	{ name: "Backup Agent", logs: [{ text: "scheduled" }, { text: "aws", icon: "AWS" }, { text: "ready", icon: "PostgreSQL" }], activity: [{ icon: "AWS", text: "S3 snapshot complete" }, { icon: "PostgreSQL", text: "WAL archived" }, { icon: "MongoDB", text: "Dump verified" }] },
	{ name: "DNS Agent", logs: [{ text: "scheduled" }, { text: "azure", icon: "Azure" }, { text: "ready", icon: "Google Cloud" }], activity: [{ icon: "Azure", text: "Updated DNS records" }, { icon: "Google Cloud", text: "CDN cache purged" }, { icon: "Datadog", text: "TTL propagated" }] },
	{ name: "Cert Agent", logs: [{ text: "scheduled" }, { text: "kubernetes", icon: "Kubernetes" }, { text: "ready", icon: "AWS" }], activity: [{ icon: "Kubernetes", text: "Renewed 8 certs" }, { icon: "AWS", text: "ACM validated" }, { icon: "PagerDuty", text: "Expiry alert cleared" }] },
	{ name: "Network Agent", logs: [{ text: "scheduled" }, { text: "terraform", icon: "Terraform" }, { text: "ready", icon: "Azure" }], activity: [{ icon: "Terraform", text: "VPC peering added" }, { icon: "Azure", text: "NSG rules updated" }, { icon: "Datadog", text: "No packet loss" }] },
	{ name: "Secret Agent", logs: [{ text: "scheduled" }, { text: "aws", icon: "AWS" }, { text: "ready", icon: "Kubernetes" }], activity: [{ icon: "AWS", text: "Rotated 6 keys" }, { icon: "Kubernetes", text: "Secrets synced" }, { icon: "Sentry", text: "No leaks detected" }] },
	{ name: "Cache Agent", logs: [{ text: "scheduled" }, { text: "redis", icon: "Redis" }, { text: "ready", icon: "Datadog" }], activity: [{ icon: "Redis", text: "Hit rate 99.4%" }, { icon: "Datadog", text: "Memory usage 62%" }, { icon: "PagerDuty", text: "All thresholds ok" }] },
	{ name: "Log Agent", logs: [{ text: "scheduled" }, { text: "datadog", icon: "Datadog" }, { text: "ready", icon: "Sentry" }], activity: [{ icon: "Datadog", text: "Indexed 1.2TB logs" }, { icon: "Sentry", text: "Grouped 3 issues" }, { icon: "PagerDuty", text: "No new alerts" }] },
	{ name: "Storage Agent", logs: [{ text: "scheduled" }, { text: "aws", icon: "AWS" }, { text: "ready", icon: "Google Cloud" }], activity: [{ icon: "AWS", text: "Lifecycle rules set" }, { icon: "Google Cloud", text: "GCS bucket synced" }, { icon: "Snowflake", text: "Stage refreshed" }] },
	{ name: "Migration Agent", logs: [{ text: "scheduled" }, { text: "postgresql", icon: "PostgreSQL" }, { text: "ready", icon: "Supabase" }], activity: [{ icon: "PostgreSQL", text: "Applied migration #42" }, { icon: "Supabase", text: "RLS policies set" }, { icon: "Redis", text: "Cache invalidated" }] },
	{ name: "Cost Agent", logs: [{ text: "scheduled" }, { text: "aws", icon: "AWS" }, { text: "ready", icon: "Azure" }], activity: [{ icon: "AWS", text: "Savings plan applied" }, { icon: "Azure", text: "Reserved 4 VMs" }, { icon: "Google Cloud", text: "Budget alert set" }] },
	{ name: "Mesh Agent", logs: [{ text: "scheduled" }, { text: "kubernetes", icon: "Kubernetes" }, { text: "ready", icon: "Docker" }], activity: [{ icon: "Kubernetes", text: "Istio sidecar injected" }, { icon: "Docker", text: "Base image updated" }, { icon: "Datadog", text: "Traces flowing" }] },
	{ name: "Queue Agent", logs: [{ text: "scheduled" }, { text: "aws", icon: "AWS" }, { text: "ready", icon: "Redis" }], activity: [{ icon: "AWS", text: "SQS depth normal" }, { icon: "Redis", text: "Pub/sub active" }, { icon: "Datadog", text: "Consumer lag 0" }] },
	{ name: "Registry Agent", logs: [{ text: "scheduled" }, { text: "docker", icon: "Docker" }, { text: "ready", icon: "GitHub" }], activity: [{ icon: "Docker", text: "Pruned 40 old tags" }, { icon: "GitHub", text: "GHCR synced" }, { icon: "AWS", text: "ECR replicated" }] },
	{ name: "Pipeline Agent", logs: [{ text: "scheduled" }, { text: "github", icon: "GitHub" }, { text: "ready", icon: "Docker" }], activity: [{ icon: "GitHub", text: "Workflow #891 passed" }, { icon: "Docker", text: "Multi-arch build" }, { icon: "Kubernetes", text: "Canary deployed" }] },
	{ name: "Alerting Agent", logs: [{ text: "scheduled" }, { text: "pagerduty", icon: "PagerDuty" }, { text: "ready", icon: "Datadog" }], activity: [{ icon: "PagerDuty", text: "Escalation updated" }, { icon: "Datadog", text: "SLO at 99.95%" }, { icon: "Sentry", text: "Alert rules synced" }] },
	{ name: "IAM Agent", logs: [{ text: "scheduled" }, { text: "aws", icon: "AWS" }, { text: "ready", icon: "Azure" }], activity: [{ icon: "AWS", text: "Audit IAM policies" }, { icon: "Azure", text: "RBAC reviewed" }, { icon: "Google Cloud", text: "SA keys rotated" }] },
	{ name: "Edge Agent", logs: [{ text: "scheduled" }, { text: "google-cloud", icon: "Google Cloud" }, { text: "ready", icon: "AWS" }], activity: [{ icon: "Google Cloud", text: "Cloud Run scaled" }, { icon: "AWS", text: "Lambda@Edge updated" }, { icon: "Datadog", text: "Edge latency 12ms" }] },
	{ name: "Sync Agent", logs: [{ text: "scheduled" }, { text: "zapier", icon: "Zapier" }, { text: "ready", icon: "Supabase" }], activity: [{ icon: "Zapier", text: "14 zaps triggered" }, { icon: "Supabase", text: "Realtime synced" }, { icon: "PostgreSQL", text: "Replication lag 0" }] },
	{ name: "Scan Agent", logs: [{ text: "scheduled" }, { text: "sentry", icon: "Sentry" }, { text: "ready", icon: "GitHub" }], activity: [{ icon: "Sentry", text: "0 critical issues" }, { icon: "GitHub", text: "Dependabot merged" }, { icon: "Docker", text: "No CVEs found" }] },
	{ name: "Perf Agent", logs: [{ text: "scheduled" }, { text: "datadog", icon: "Datadog" }, { text: "ready", icon: "Redis" }], activity: [{ icon: "Datadog", text: "APM traces analyzed" }, { icon: "Redis", text: "Slow log cleared" }, { icon: "PostgreSQL", text: "Query plan optimized" }] },
	{ name: "Failover Agent", logs: [{ text: "scheduled" }, { text: "aws", icon: "AWS" }, { text: "ready", icon: "Azure" }], activity: [{ icon: "AWS", text: "Route53 health ok" }, { icon: "Azure", text: "Traffic Manager ok" }, { icon: "PagerDuty", text: "DR test passed" }] },
	{ name: "Quota Agent", logs: [{ text: "scheduled" }, { text: "google-cloud", icon: "Google Cloud" }, { text: "ready", icon: "AWS" }], activity: [{ icon: "Google Cloud", text: "Quota usage 41%" }, { icon: "AWS", text: "Service limits ok" }, { icon: "Azure", text: "No throttling" }] },
	{ name: "Cleanup Agent", logs: [{ text: "scheduled" }, { text: "kubernetes", icon: "Kubernetes" }, { text: "ready", icon: "Docker" }], activity: [{ icon: "Kubernetes", text: "Evicted 8 pods" }, { icon: "Docker", text: "Dangling images rm" }, { icon: "AWS", text: "Orphan EBS deleted" }] },
	{ name: "Proxy Agent", logs: [{ text: "scheduled" }, { text: "kubernetes", icon: "Kubernetes" }, { text: "ready", icon: "Terraform" }], activity: [{ icon: "Kubernetes", text: "Ingress rules synced" }, { icon: "Terraform", text: "LB config applied" }, { icon: "Datadog", text: "502 rate 0%" }] },
	{ name: "Config Agent", logs: [{ text: "scheduled" }, { text: "terraform", icon: "Terraform" }, { text: "ready", icon: "Kubernetes" }], activity: [{ icon: "Terraform", text: "Drift detected: 0" }, { icon: "Kubernetes", text: "ConfigMaps updated" }, { icon: "GitHub", text: "Gitops in sync" }] },
	{ name: "Tag Agent", logs: [{ text: "scheduled" }, { text: "aws", icon: "AWS" }, { text: "ready", icon: "Azure" }], activity: [{ icon: "AWS", text: "Tagged 120 resources" }, { icon: "Azure", text: "Policy enforced" }, { icon: "Google Cloud", text: "Labels synced" }] },
	{ name: "Trace Agent", logs: [{ text: "scheduled" }, { text: "datadog", icon: "Datadog" }, { text: "ready", icon: "Sentry" }], activity: [{ icon: "Datadog", text: "Trace sampling 100%" }, { icon: "Sentry", text: "Session replay on" }, { icon: "PagerDuty", text: "Incident timeline" }] },
	{ name: "Volume Agent", logs: [{ text: "scheduled" }, { text: "kubernetes", icon: "Kubernetes" }, { text: "ready", icon: "AWS" }], activity: [{ icon: "Kubernetes", text: "PVC resized" }, { icon: "AWS", text: "EFS throughput ok" }, { icon: "Datadog", text: "Disk usage 58%" }] },
	{ name: "Patch Agent", logs: [{ text: "scheduled" }, { text: "docker", icon: "Docker" }, { text: "ready", icon: "Sentry" }], activity: [{ icon: "Docker", text: "Base images rebuilt" }, { icon: "Sentry", text: "0 vulnerabilities" }, { icon: "GitHub", text: "Security PR merged" }] },
	{ name: "Replica Agent", logs: [{ text: "scheduled" }, { text: "postgresql", icon: "PostgreSQL" }, { text: "ready", icon: "MongoDB" }], activity: [{ icon: "PostgreSQL", text: "Replica lag 0ms" }, { icon: "MongoDB", text: "Replica set healthy" }, { icon: "Redis", text: "Sentinel quorum ok" }] },
	{ name: "Test Agent", logs: [{ text: "scheduled" }, { text: "github", icon: "GitHub" }, { text: "ready", icon: "Sentry" }], activity: [{ icon: "GitHub", text: "E2E suite passed" }, { icon: "Sentry", text: "Error budget 98%" }, { icon: "Datadog", text: "Synthetics green" }] },
	{ name: "Flux Agent", logs: [{ text: "scheduled" }, { text: "kubernetes", icon: "Kubernetes" }, { text: "ready", icon: "GitHub" }], activity: [{ icon: "Kubernetes", text: "Flux reconciled" }, { icon: "GitHub", text: "Kustomize synced" }, { icon: "Docker", text: "Image policy ok" }] },
	{ name: "Billing Agent", logs: [{ text: "scheduled" }, { text: "stripe", icon: "Stripe" }, { text: "ready", icon: "AWS" }], activity: [{ icon: "Stripe", text: "30 invoices sent" }, { icon: "AWS", text: "Cost anomaly: none" }, { icon: "Snowflake", text: "Credit usage ok" }] },
	{ name: "Health Agent", logs: [{ text: "scheduled" }, { text: "kubernetes", icon: "Kubernetes" }, { text: "ready", icon: "Datadog" }], activity: [{ icon: "Kubernetes", text: "All probes passing" }, { icon: "Datadog", text: "Uptime 99.99%" }, { icon: "PagerDuty", text: "No incidents" }] },
	{ name: "Auth Agent", logs: [{ text: "scheduled" }, { text: "supabase", icon: "Supabase" }, { text: "ready", icon: "AWS" }], activity: [{ icon: "Supabase", text: "JWT keys rotated" }, { icon: "AWS", text: "Cognito pools ok" }, { icon: "Azure", text: "AD sync complete" }] },
	{ name: "CDN Agent", logs: [{ text: "scheduled" }, { text: "aws", icon: "AWS" }, { text: "ready", icon: "Google Cloud" }], activity: [{ icon: "AWS", text: "CloudFront invalidated" }, { icon: "Google Cloud", text: "Cloud CDN warm" }, { icon: "Datadog", text: "Cache hit 96%" }] },
	{ name: "Schema Agent", logs: [{ text: "scheduled" }, { text: "postgresql", icon: "PostgreSQL" }, { text: "ready", icon: "Snowflake" }], activity: [{ icon: "PostgreSQL", text: "Index rebuilt" }, { icon: "Snowflake", text: "Schema evolved" }, { icon: "Supabase", text: "Types generated" }] },
];
