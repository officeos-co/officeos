# Skills Roadmap — Top 100 Skills for Release

> Sourced from [ClawhHub](https://clawhub.ai/skills?sort=downloads&nonSuspicious=true) live API data (April 2026) and [awesome-openclaw-skills](https://github.com/VoltAgent/awesome-openclaw-skills) (5,400+ skills). Each skill published as a public GitHub repo under `github.com/harro-skills/` for trust and discoverability.

**DONE** = already in `packages/skills/` | **TODO** = to be implemented

---

## 1. Agent Core & Memory

> Prompt-only skills (self-improving, ontology, deep-research, prompt-guard) removed — skills must wrap a real CLI/SDK/API.

1. **browser** — Headless browser automation via Playwright with accessibility tree snapshots (85K downloads) `harro-skills/skill-browser` **DONE**
2. **web-search** — Unified multi-engine search: Google, Bing, Reddit, arXiv, HN and more (116K downloads) `harro-skills/skill-web-search` TODO
3. **web-scraper** — Extract structured data from web pages with anti-bot bypass (6.7K downloads) `harro-skills/skill-web-scraper` TODO
4. **perplexity** — AI-powered deep search via Perplexity API `harro-skills/skill-perplexity` TODO
5. **exa** — Neural semantic search and content extraction via Exa AI (25K downloads) `harro-skills/skill-exa` TODO
6. **api-gateway** — Connect to 100+ APIs with managed OAuth: Google, Microsoft, Slack, HubSpot, etc. (67K downloads) `harro-skills/skill-api-gateway` TODO

## 2. Productivity & Tasks

11. **notion** — Notion page, database, and block operations `harro-skills/skill-notion` **DONE**
13. **todoist** — Task and project management via Todoist API `harro-skills/skill-todoist` TODO
14. **linear** — Issue tracking, project management, and sprint workflows via Linear API `harro-skills/skill-linear` TODO
15. **jira** — Jira issue and project management via Atlassian REST API `harro-skills/skill-jira` TODO
16. **asana** — Task and project management via Asana REST API `harro-skills/skill-asana` TODO
17. **clickup** — Tasks, docs, time tracking, and comments via ClickUp API `harro-skills/skill-clickup` TODO
18. **trello** — Board, list, and card management via Trello API `harro-skills/skill-trello` TODO
19. **google-calendar** — Google Calendar event management via Google API `harro-skills/skill-google-calendar` TODO
22. **excel** — Read, write, edit, and format Excel/.xlsx with formulas and formatting (52K downloads) `harro-skills/skill-excel` TODO

## 3. Communication & Email

> Channel-specific skills (Slack, Discord, Teams, Telegram, WhatsApp, Twilio SMS, iMessage, Intercom) removed — native channel integrations handle messaging.

23. **gmail** — Gmail read, send, search, label, and draft management (5.7K downloads) `harro-skills/skill-gmail` TODO
24. **imap-smtp-email** — Read and send email via IMAP/SMTP, works with any email provider (37K downloads) `harro-skills/skill-imap-smtp-email` TODO

## 4. Developer Tools & Git

33. **github** — GitHub repo, issue, PR, and code management `harro-skills/skill-github` **DONE**
34. **git** — Git commits, branches, rebases, merges, conflict resolution, team workflows (11K downloads) `harro-skills/skill-git` TODO
35. **gitlab** — GitLab project, MR, and pipeline management via glab CLI `harro-skills/skill-gitlab` TODO
36. **bitbucket** — Bitbucket repository and pull request operations `harro-skills/skill-bitbucket` TODO
37. **github-actions** — Create, debug, and manage GitHub Actions workflows `harro-skills/skill-github-actions` TODO
38. **jenkins** — Jenkins CI/CD job management via REST API `harro-skills/skill-jenkins` TODO
39. **azure-devops** — Azure DevOps projects, repos, PRs, work items, and builds `harro-skills/skill-azure-devops` TODO
40. **sentry** — Error tracking, issue management, and release monitoring via Sentry API `harro-skills/skill-sentry` TODO
41. **playwright** — Full browser automation via Playwright MCP — navigate, click, fill, screenshot (26K downloads) `harro-skills/skill-playwright` TODO

## 5. Cloud & Infrastructure

43. **aws** — AWS CLI operations — EC2, S3, Lambda, ECS, IAM, CloudWatch `harro-skills/skill-aws` TODO
44. **gcp** — Google Cloud Platform — Compute, GKE, Cloud Run, BigQuery `harro-skills/skill-gcp` TODO
45. **azure** — Azure resource management via Azure CLI `harro-skills/skill-azure` TODO
46. **docker** — Docker containers, images, Compose stacks, networking, volumes (10K downloads) `harro-skills/skill-docker` TODO
47. **kubernetes** — K8s cluster operations — pods, deployments, services, logs `harro-skills/skill-kubernetes` TODO
48. **terraform** — Terraform plan, apply, and module management `harro-skills/skill-terraform` TODO
49. **cloudflare** — DNS, caching, security rules, Workers, and SSL/TLS management `harro-skills/skill-cloudflare` TODO
50. **vercel** — Vercel project deployment, domains, and environment management `harro-skills/skill-vercel` TODO
51. **railway** — Deploy and manage Railway services via CLI `harro-skills/skill-railway` TODO
52. **digital-ocean** — Droplet, database, and domain management `harro-skills/skill-digital-ocean` TODO

## 6. Databases & Storage

53. **postgres** — PostgreSQL query execution, schema management, and migrations `harro-skills/skill-postgres` TODO
54. **mysql** — MySQL/MariaDB query execution and database management `harro-skills/skill-mysql` TODO
55. **mongodb** — MongoDB CRUD, aggregation, and collection management `harro-skills/skill-mongodb` TODO
56. **redis** — Redis key-value operations, pub/sub, and cache management `harro-skills/skill-redis` TODO
57. **supabase** — Supabase database, auth, vector search, and storage operations `harro-skills/skill-supabase` TODO
58. **firebase** — Firebase Firestore, Auth, and Cloud Functions management `harro-skills/skill-firebase` TODO
59. **s3** — AWS S3 bucket and object operations (upload, download, list, delete) `harro-skills/skill-s3` TODO

## 7. CRM & Sales

61. **salesforce** — Salesforce CRM — leads, contacts, opportunities, and reports `harro-skills/skill-salesforce` TODO
62. **hubspot** — HubSpot CRM contacts, deals, companies, and marketing automation `harro-skills/skill-hubspot` TODO
63. **pipedrive** — Pipedrive CRM deal and contact management `harro-skills/skill-pipedrive` TODO
64. **zendesk** — Zendesk ticket, user, and knowledge base management `harro-skills/skill-zendesk` TODO
65. **freshdesk** — Freshdesk support ticket and customer management `harro-skills/skill-freshdesk` TODO
66. **attio** — Attio CRM API with batch operations and relationship management `harro-skills/skill-attio` TODO
67. **klaviyo** — Klaviyo profiles, lists, segments, campaigns, flows, events, templates (15K downloads) `harro-skills/skill-klaviyo` TODO
68. **apollo** — Apollo.io lead enrichment and prospecting `harro-skills/skill-apollo` TODO

## 8. Marketing & Content

69. **twitter** — X/Twitter — search posts, read timelines, compose and post tweets (9.4K downloads) `harro-skills/skill-twitter` TODO
72. **linkedin** — LinkedIn profile data, post sharing, and ad management via API (9.5K downloads) `harro-skills/skill-linkedin` TODO
73. **social-scheduler** — Schedule posts to 28+ channels: X, LinkedIn, Reddit, Instagram, TikTok, etc. (8.5K downloads) `harro-skills/skill-social-scheduler` TODO
74. **google-analytics** — Google Analytics 4 — tracking setup, reports, and audience data `harro-skills/skill-google-analytics` TODO
75. **meta-ads** — Meta (Facebook/Instagram) advertising campaign management `harro-skills/skill-meta-ads` TODO
76. **canva** — Create, export, and manage Canva designs via Connect API `harro-skills/skill-canva` TODO

## 9. Documents & Files

77. **word** — Create, inspect, and edit Microsoft Word/DOCX with styles, tables, tracked changes (58K downloads) `harro-skills/skill-word` TODO
78. **powerpoint** — Create, inspect, and edit PowerPoint/PPTX with layouts, templates, placeholders (32K downloads) `harro-skills/skill-powerpoint` TODO
79. **google-drive** — Google Drive file operations — upload, download, share, organize `harro-skills/skill-google-drive` TODO
80. **google-sheets** — Google Sheets read, write, and formula operations `harro-skills/skill-google-sheets` TODO
81. **pdf** — PDF read, create, merge, split, text extraction, and OCR (7.6K downloads) `harro-skills/skill-pdf` TODO
82. **csv** — CSV/JSON processing, transformation, analysis, and reporting `harro-skills/skill-csv` TODO
83. **airtable** — Airtable base, table, and record operations via REST API `harro-skills/skill-airtable` TODO
84. **confluence** — Atlassian Confluence page and space management `harro-skills/skill-confluence` TODO

## 10. Monitoring & Analytics

85. **grafana** — Grafana dashboard queries, alerts, traces — 16 agent tools `harro-skills/skill-grafana` TODO
86. **datadog** — Datadog monitoring — metrics, logs, APM, and alerting `harro-skills/skill-datadog` TODO
87. **pagerduty** — PagerDuty incident management and on-call schedules `harro-skills/skill-pagerduty` TODO
88. **posthog** — PostHog product analytics — events, funnels, and feature flags `harro-skills/skill-posthog` TODO
89. **prometheus** — Prometheus metric queries and alert rule management `harro-skills/skill-prometheus` TODO
90. **log-search** — Unified log search across Loki, Elasticsearch, and CloudWatch `harro-skills/skill-log-search` TODO
91. **newrelic** — New Relic APM, infrastructure, and log management `harro-skills/skill-newrelic` TODO

## 11. Finance & Payments

93. **stripe** — Stripe payments — charges, subscriptions, customers, and invoices `harro-skills/skill-stripe` TODO
94. **quickbooks** — QuickBooks accounting — invoices, expenses, and reports `harro-skills/skill-quickbooks` TODO
95. **stock-analysis** — Real-time stock quotes, portfolio analysis, and screener (20K downloads) `harro-skills/skill-stock-analysis` TODO
96. **crypto** — Cryptocurrency market data, portfolio tracking, and trading (109K downloads) `harro-skills/skill-crypto` TODO

## 12. Media & Generation

97. **image-gen** — AI image generation/editing via Gemini, DALL-E, or Stable Diffusion (85K downloads) `harro-skills/skill-image-gen` TODO
98. **video-transcript** — Download videos, audio, subtitles, and clean transcripts from YouTube et al. (10K downloads) `harro-skills/skill-video-transcript` TODO
99. **screenshot** — Capture and compare screenshots of screens, windows, regions, web pages (12K downloads) `harro-skills/skill-screenshot` TODO
100.  **speech** — Text-to-speech and speech-to-text via Whisper / ElevenLabs APIs (20K downloads) `harro-skills/skill-speech` TODO

---

## Totals

- 79 skills across 12 categories (10 channel skills + 11 prompt-only skills removed)
- 5 done, 74 to implement
- Top by ClawhHub downloads: self-improving (384K), ontology (162K), web-search (116K), crypto (109K), image-gen (85K), browser (85K), api-gateway (67K), word (58K), excel (52K)

## Data Sources

- ClawhHub API: `wry-manatee-359.convex.cloud` — `skills:listPublicPageV4`, sorted by downloads desc, `nonSuspiciousOnly=true`
- [VoltAgent/awesome-openclaw-skills](https://github.com/VoltAgent/awesome-openclaw-skills) — 5,400+ skills, 25 categories
- [sundial-org/awesome-openclaw-skills](https://github.com/sundial-org/awesome-openclaw-skills) — 913 curated top skills

---

## ClawhHub Source Repos & OSS References

> For each planned skill: the ClawhHub listing (original skill), the underlying open-source CLI/library/MCP server it wraps, and flags for skills with no clear OSS base.

### 1. Agent Core & Memory

1. **browser** (DONE) — OSS: [microsoft/playwright](https://github.com/microsoft/playwright). Playwright MCP: `@anthropic/mcp-server-playwright`
2. **web-search** — ClawhHub: [billyutw/web-search](https://clawhub.ai/billyutw/web-search) — OSS: DuckDuckGo API / [searxng/searxng](https://github.com/searxng/searxng). Alt: `robbyczgw-cla/web-search-plus` uses Serper + Tavily
3. **web-scraper** — OSS: [apify/crawlee](https://github.com/apify/crawlee) or [AlessandroZanella/mcp-server-webscraper](https://github.com/AlessandroZanella/mcp-server-webscraper). Crawlee is the main OSS scraping framework
4. **perplexity** — ClawhHub: [zats/perplexity](https://clawhub.ai/zats/perplexity) — ⚠️ PROPRIETARY API. Perplexity API is closed
5. **exa** — ⚠️ PROPRIETARY API. Exa AI API is closed, SDK exists but API is proprietary
6. **api-gateway** — ClawhHub: [byungkyu/api-gateway](https://clawhub.ai/byungkyu/api-gateway) — OSS: [maton-ai/api-gateway-skill](https://github.com/maton-ai/api-gateway-skill). Maton platform is commercial

### 2. Productivity & Tasks

11. **notion** (DONE) — OSS: [makenotion/notion-sdk-js](https://github.com/makenotion/notion-sdk-js). Official Notion SDK
13. **todoist** — OSS: [Doist/todoist-api-typescript](https://github.com/Doist/todoist-api-typescript). Official Todoist SDK
14. **linear** — OSS: [linear/linear](https://github.com/linear/linear). Official Linear SDK (MIT)
15. **jira** — REST API; no official CLI. Community: [go-jira/jira](https://github.com/go-jira/jira)
16. **asana** — OSS: [Asana/node-asana](https://github.com/Asana/node-asana). Official Asana SDK
17. **clickup** — ⚠️ No official OSS SDK. REST API only
18. **trello** — OSS: [norberteder/trello](https://github.com/norberteder/trello). Community Node.js client, Trello API is REST-based
19. **google-calendar** — OSS: [googleapis/google-api-nodejs-client](https://github.com/googleapis/google-api-nodejs-client). Official Google API client
22. **excel** — ClawhHub: [ivangdavila/excel-xlsx](https://clawhub.ai/ivangdavila/excel-xlsx) — OSS: [exceljs/exceljs](https://github.com/exceljs/exceljs) (MIT) or [SheetJS/sheetjs](https://github.com/SheetJS/sheetjs) (Apache-2.0)

### 3. Communication & Email

> Channel-specific skills removed — native channel integrations.

23. **gmail** — ClawhHub: [byungkyu/gmail](https://clawhub.ai/byungkyu/gmail) — OSS: [googleapis/google-api-nodejs-client](https://github.com/googleapis/google-api-nodejs-client). Official Google API client
24. **imap-smtp-email** — ClawhHub: [gzlicanyi/imap-smtp-email](https://clawhub.ai/gzlicanyi/imap-smtp-email) — OSS: [nodemailer/nodemailer](https://github.com/nodemailer/nodemailer) + [mscdex/node-imap](https://github.com/mscdex/node-imap). Both MIT

### 4. Developer Tools & Git

33. **github** (DONE) — OSS: [octokit/octokit.js](https://github.com/octokit/octokit.js) + [cli/cli](https://github.com/cli/cli). GitHub CLI + Octokit SDK (both OSS)
34. **git** — OSS: [git/git](https://github.com/git/git). Git CLI (GPL-2.0)
35. **gitlab** — OSS: [profclems/glab](https://github.com/profclems/glab). GLab CLI (MIT)
36. **bitbucket** — ⚠️ NO OFFICIAL CLI. REST API only. Community: [sohamganatra/bitbucket-automation](https://github.com/openclaw/skills/tree/main/skills/sohamganatra/bitbucket-automation)
37. **github-actions** — OSS: [cli/cli](https://github.com/cli/cli) (`gh workflow`, `gh run`). Part of GitHub CLI
38. **jenkins** — OSS: [Jenkins built-in CLI](https://www.jenkins.io/doc/book/managing/cli/). Jenkins is OSS (MIT)
39. **azure-devops** — OSS: [Azure/azure-devops-cli-extension](https://github.com/Azure/azure-devops-cli-extension). Official CLI extension
40. **sentry** — OSS: [getsentry/sentry-cli](https://github.com/getsentry/sentry-cli). Official Sentry CLI (BSD-3)
41. **playwright** — OSS: [microsoft/playwright](https://github.com/microsoft/playwright). Playwright (Apache-2.0)

### 5. Cloud & Infrastructure

43. **aws** — OSS: [aws/aws-cli](https://github.com/aws/aws-cli). Official AWS CLI (Apache-2.0)
44. **gcp** — [google-cloud-sdk](https://cloud.google.com/sdk). `gcloud` CLI, source available but not fully OSS
45. **azure** — OSS: [Azure/azure-cli](https://github.com/Azure/azure-cli). Official Azure CLI (MIT)
46. **docker** — OSS: [docker/cli](https://github.com/docker/cli). Docker CLI (Apache-2.0)
47. **kubernetes** — OSS: [kubernetes/kubectl](https://github.com/kubernetes/kubectl). kubectl (Apache-2.0)
48. **terraform** — OSS: [hashicorp/terraform](https://github.com/hashicorp/terraform). Terraform CLI (BSL-1.1, was OSS). Alt: [opentofu/opentofu](https://github.com/opentofu/opentofu) (MPL-2.0)
49. **cloudflare** — OSS: [cloudflare/workers-sdk](https://github.com/cloudflare/workers-sdk) (Wrangler). Wrangler CLI (MIT/Apache-2.0)
50. **vercel** — OSS: [vercel/vercel](https://github.com/vercel/vercel). Vercel CLI (Apache-2.0)
51. **railway** — OSS: [railwayapp/cli](https://github.com/railwayapp/cli). Railway CLI (MIT)
52. **digital-ocean** — OSS: [digitalocean/doctl](https://github.com/digitalocean/doctl). doctl CLI (Apache-2.0)

### 6. Databases & Storage

53. **postgres** — OSS: [PostgreSQL `psql`](https://www.postgresql.org/) + [brianc/node-postgres](https://github.com/brianc/node-postgres). psql CLI + `pg` Node.js (both OSS)
54. **mysql** — OSS: [mysql/mysql-server](https://github.com/mysql/mysql-server) + [mysqljs/mysql](https://github.com/mysqljs/mysql). mysql CLI + Node.js client (both OSS)
55. **mongodb** — OSS: [mongodb-js/mongosh](https://github.com/mongodb-js/mongosh) + [mongodb/node-mongodb-native](https://github.com/mongodb/node-mongodb-native). mongosh + driver (both OSS)
56. **redis** — OSS: [redis/redis](https://github.com/redis/redis) + [redis/node-redis](https://github.com/redis/node-redis). redis-cli + Node.js client (both OSS)
57. **supabase** — OSS: [supabase/cli](https://github.com/supabase/cli) + [supabase/supabase-js](https://github.com/supabase/supabase-js). CLI + SDK (both MIT)
58. **firebase** — OSS: [firebase/firebase-tools](https://github.com/firebase/firebase-tools). Firebase CLI (MIT)
59. **s3** — OSS: [aws/aws-cli](https://github.com/aws/aws-cli) (`aws s3`). Part of AWS CLI

### 7. CRM & Sales

61. **salesforce** — OSS: [forcedotcom/cli](https://github.com/forcedotcom/cli) (`sf` CLI). Official Salesforce CLI (BSD-3)
62. **hubspot** — OSS: [HubSpot/hubspot-api-nodejs](https://github.com/HubSpot/hubspot-api-nodejs). Official SDK
63. **pipedrive** — OSS: [pipedrive/client-nodejs](https://github.com/pipedrive/client-nodejs). Official SDK (MIT)
64. **zendesk** — OSS: [blakmatrix/node-zendesk](https://github.com/blakmatrix/node-zendesk). Community SDK, no official Node.js SDK
65. **freshdesk** — ⚠️ NO OSS SDK. REST API only, no maintained Node.js SDK
66. **attio** — ⚠️ NO OSS SDK. REST API only, recently launched
67. **klaviyo** — OSS: [klaviyo/klaviyo-api-node](https://github.com/klaviyo/klaviyo-api-node). Official SDK
68. **apollo** — ⚠️ NO OSS SDK. Apollo.io REST API only, no official/community SDK

### 8. Marketing & Content

69. **twitter** — ⚠️ PROPRIETARY API. X/Twitter API is paid/restricted. Community: [PLhery/node-twitter-api-v2](https://github.com/PLhery/node-twitter-api-v2)
72. **linkedin** — ⚠️ PROPRIETARY API. LinkedIn API is restricted/commercial, no public OSS SDK with full access
73. **social-scheduler** — ⚠️ NO OSS CLI. Wraps multiple commercial APIs (Buffer, Hootsuite)
74. **google-analytics** — OSS: [googleapis/google-api-nodejs-client](https://github.com/googleapis/google-api-nodejs-client). GA4 Data API via Google SDK
75. **meta-ads** — OSS: [facebook/facebook-nodejs-business-sdk](https://github.com/facebook/facebook-nodejs-business-sdk). Official Meta Business SDK
76. **canva** — ⚠️ PROPRIETARY API. Canva Connect API is closed/commercial

### 9. Documents & Files

77. **word** — ClawhHub: [ivangdavila/word-docx](https://clawhub.ai/ivangdavila/word-docx) — OSS: [dolanmiu/docx](https://github.com/dolanmiu/docx) (MIT) for Node.js
78. **powerpoint** — ClawhHub: [ivangdavila/powerpoint-pptx](https://clawhub.ai/ivangdavila/powerpoint-pptx) — OSS: [gitbrent/PptxGenJS](https://github.com/gitbrent/PptxGenJS) (MIT)
79. **google-drive** — OSS: [googleapis/google-api-nodejs-client](https://github.com/googleapis/google-api-nodejs-client). Google Drive API via official SDK
80. **google-sheets** — OSS: [googleapis/google-api-nodejs-client](https://github.com/googleapis/google-api-nodejs-client). Google Sheets API via official SDK
81. **pdf** — OSS: [Hopding/pdf-lib](https://github.com/Hopding/pdf-lib) (MIT) for creation, [mozilla/pdf.js](https://github.com/nicolo-ribaudo/pdfjs-dist) for reading
82. **csv** — OSS: [mholt/PapaParse](https://github.com/mholt/PapaParse) (MIT). Standard CSV parsing
83. **airtable** — OSS: [Airtable/airtable.js](https://github.com/Airtable/airtable.js). Official SDK (MIT)
84. **confluence** — REST API. No official Node.js SDK. ⚠️ Community clients only

### 10. Monitoring & Analytics

85. **grafana** — OSS: [grafana/grafana](https://github.com/grafana/grafana). Grafana HTTP API (AGPL-3.0)
86. **datadog** — OSS: [DataDog/datadog-api-client-typescript](https://github.com/DataDog/datadog-api-client-typescript). Official SDK (Apache-2.0)
87. **pagerduty** — OSS: [PagerDuty/pdjs](https://github.com/PagerDuty/pdjs). Official JS client
88. **posthog** — OSS: [PostHog/posthog-js](https://github.com/PostHog/posthog-js). Official SDK (MIT)
89. **prometheus** — OSS: [prometheus/prometheus](https://github.com/prometheus/prometheus). PromQL HTTP API (Apache-2.0)
90. **log-search** — ⚠️ NO SINGLE OSS CLI. Multi-backend (Loki/ES/CloudWatch), each has its own client
91. **newrelic** — OSS: [newrelic/node-newrelic](https://github.com/newrelic/node-newrelic). Official Node.js agent (Apache-2.0), API via REST

### 11. Finance & Payments

93. **stripe** — OSS: [stripe/stripe-node](https://github.com/stripe/stripe-node). Official SDK (MIT)
94. **quickbooks** — OSS: [intuit/oauth-jsclient](https://github.com/intuit/oauth-jsclient). Official OAuth client only. ⚠️ No full QuickBooks Node.js SDK
95. **stock-analysis** — ClawhHub: [udiedrichsen/stock-analysis](https://clawhub.ai/udiedrichsen/stock-analysis) — OSS: [gadicc/node-yahoo-finance2](https://github.com/gadicc/node-yahoo-finance2) (Node.js) or [ranaroussi/yfinance](https://github.com/ranaroussi/yfinance) (Python)
96. **crypto** — OSS: [ccxt/ccxt](https://github.com/ccxt/ccxt). CCXT (MIT) — unified crypto exchange API

### 12. Media & Generation

97. **image-gen** — ClawhHub: [steipete/openai-image-gen](https://clawhub.ai/steipete/openai-image-gen) — ⚠️ PROPRIETARY APIs. DALL-E/Gemini/SD APIs are commercial. SD has OSS models but generation APIs are commercial
98. **video-transcript** — OSS: [yt-dlp/yt-dlp](https://github.com/yt-dlp/yt-dlp) + [openai/whisper](https://github.com/openai/whisper). yt-dlp (Unlicense) + Whisper (MIT), both fully OSS
99. **screenshot** — ⚠️ No dominant OSS CLI. macOS `screencapture` or Playwright can screenshot
100.  **speech** — OSS (STT): [openai/whisper](https://github.com/openai/whisper) (MIT). TTS: ⚠️ ElevenLabs API is proprietary

---

## Skills by Source Type

### ✅ Open Source — spec-drive from CLI/SDK repo

- **1. browser** — [microsoft/playwright](https://github.com/microsoft/playwright) (DONE)
- **5. web-search** — [searxng/searxng](https://github.com/searxng/searxng)
- **6. web-scraper** — [apify/crawlee](https://github.com/apify/crawlee)
- **11. notion** — [makenotion/notion-sdk-js](https://github.com/makenotion/notion-sdk-js) (DONE)
- **13. todoist** — [Doist/todoist-api-typescript](https://github.com/Doist/todoist-api-typescript)
- **14. linear** — [linear/linear](https://github.com/linear/linear)
- **16. asana** — [Asana/node-asana](https://github.com/Asana/node-asana)
- **18. trello** — [norberteder/trello](https://github.com/norberteder/trello)
- **21. google-calendar** — [googleapis/google-api-nodejs-client](https://github.com/googleapis/google-api-nodejs-client)
- **22. excel** — [exceljs/exceljs](https://github.com/exceljs/exceljs)
- **23. gmail** — [googleapis/google-api-nodejs-client](https://github.com/googleapis/google-api-nodejs-client)
- **24. imap-smtp-email** — [nodemailer/nodemailer](https://github.com/nodemailer/nodemailer) + [mscdex/node-imap](https://github.com/mscdex/node-imap)
- **33. github** — [octokit/octokit.js](https://github.com/octokit/octokit.js) + [cli/cli](https://github.com/cli/cli) (DONE)
- **34. git** — [git/git](https://github.com/git/git)
- **35. gitlab** — [profclems/glab](https://github.com/profclems/glab)
- **37. github-actions** — [cli/cli](https://github.com/cli/cli)
- **38. jenkins** — [Jenkins CLI](https://www.jenkins.io/doc/book/managing/cli/)
- **39. azure-devops** — [Azure/azure-devops-cli-extension](https://github.com/Azure/azure-devops-cli-extension)
- **40. sentry** — [getsentry/sentry-cli](https://github.com/getsentry/sentry-cli)
- **42. playwright** — [microsoft/playwright](https://github.com/microsoft/playwright)
- **43. aws** — [aws/aws-cli](https://github.com/aws/aws-cli)
- **44. gcp** — [google-cloud-sdk](https://cloud.google.com/sdk)
- **45. azure** — [Azure/azure-cli](https://github.com/Azure/azure-cli)
- **46. docker** — [docker/cli](https://github.com/docker/cli)
- **47. kubernetes** — [kubernetes/kubectl](https://github.com/kubernetes/kubectl)
- **48. terraform** — [hashicorp/terraform](https://github.com/hashicorp/terraform) or [opentofu/opentofu](https://github.com/opentofu/opentofu)
- **49. cloudflare** — [cloudflare/workers-sdk](https://github.com/cloudflare/workers-sdk)
- **50. vercel** — [vercel/vercel](https://github.com/vercel/vercel)
- **51. railway** — [railwayapp/cli](https://github.com/railwayapp/cli)
- **52. digital-ocean** — [digitalocean/doctl](https://github.com/digitalocean/doctl)
- **53. postgres** — [brianc/node-postgres](https://github.com/brianc/node-postgres)
- **54. mysql** — [mysqljs/mysql](https://github.com/mysqljs/mysql)
- **55. mongodb** — [mongodb-js/mongosh](https://github.com/mongodb-js/mongosh)
- **56. redis** — [redis/node-redis](https://github.com/redis/node-redis)
- **57. supabase** — [supabase/cli](https://github.com/supabase/cli)
- **58. firebase** — [firebase/firebase-tools](https://github.com/firebase/firebase-tools)
- **59. s3** — [aws/aws-cli](https://github.com/aws/aws-cli)
- **61. salesforce** — [forcedotcom/cli](https://github.com/forcedotcom/cli)
- **62. hubspot** — [HubSpot/hubspot-api-nodejs](https://github.com/HubSpot/hubspot-api-nodejs)
- **63. pipedrive** — [pipedrive/client-nodejs](https://github.com/pipedrive/client-nodejs)
- **64. zendesk** — [blakmatrix/node-zendesk](https://github.com/blakmatrix/node-zendesk)
- **67. klaviyo** — [klaviyo/klaviyo-api-node](https://github.com/klaviyo/klaviyo-api-node)
- **74. google-analytics** — [googleapis/google-api-nodejs-client](https://github.com/googleapis/google-api-nodejs-client)
- **75. meta-ads** — [facebook/facebook-nodejs-business-sdk](https://github.com/facebook/facebook-nodejs-business-sdk)
- **77. word** — [dolanmiu/docx](https://github.com/dolanmiu/docx)
- **78. powerpoint** — [gitbrent/PptxGenJS](https://github.com/gitbrent/PptxGenJS)
- **79. google-drive** — [googleapis/google-api-nodejs-client](https://github.com/googleapis/google-api-nodejs-client)
- **80. google-sheets** — [googleapis/google-api-nodejs-client](https://github.com/googleapis/google-api-nodejs-client)
- **81. pdf** — [Hopding/pdf-lib](https://github.com/Hopding/pdf-lib)
- **82. csv** — [mholt/PapaParse](https://github.com/mholt/PapaParse)
- **83. airtable** — [Airtable/airtable.js](https://github.com/Airtable/airtable.js)
- **85. grafana** — [grafana/grafana](https://github.com/grafana/grafana)
- **86. datadog** — [DataDog/datadog-api-client-typescript](https://github.com/DataDog/datadog-api-client-typescript)
- **87. pagerduty** — [PagerDuty/pdjs](https://github.com/PagerDuty/pdjs)
- **88. posthog** — [PostHog/posthog-js](https://github.com/PostHog/posthog-js)
- **89. prometheus** — [prometheus/prometheus](https://github.com/prometheus/prometheus)
- **92. newrelic** — [newrelic/node-newrelic](https://github.com/newrelic/node-newrelic)
- **93. stripe** — [stripe/stripe-node](https://github.com/stripe/stripe-node)
- **95. stock-analysis** — [gadicc/node-yahoo-finance2](https://github.com/gadicc/node-yahoo-finance2)
- **96. crypto** — [ccxt/ccxt](https://github.com/ccxt/ccxt)
- **98. video-transcript** — [yt-dlp/yt-dlp](https://github.com/yt-dlp/yt-dlp) + [openai/whisper](https://github.com/openai/whisper)

**Total: 67 skills** (5 done, 62 to implement)

### ⚠️ Proprietary API — spec-drive from API docs, needs paid API keys

- **7. perplexity** — Perplexity API (closed)
- **8. exa** — Exa AI API (closed)
- **10. api-gateway** — [maton-ai/api-gateway-skill](https://github.com/maton-ai/api-gateway-skill), Maton platform is commercial
- **15. jira** — Atlassian REST API, no official Node.js SDK
- **71. twitter** — X/Twitter API (paid, restricted). Community: [PLhery/node-twitter-api-v2](https://github.com/PLhery/node-twitter-api-v2)
- **72. linkedin** — LinkedIn API (restricted, no public access)
- **76. canva** — Canva Connect API (closed)
- **84. confluence** — Atlassian REST API, no official Node.js SDK
- **97. image-gen** — DALL-E/Gemini APIs (commercial). SD has OSS models but APIs are commercial
- **100. speech (TTS)** — ElevenLabs API (commercial). STT via Whisper is OSS

**Total: 10 skills**

### ⚠️ No maintained OSS SDK — need to write raw REST client

- **17. clickup** — no official SDK
- **36. bitbucket** — no official CLI
- **65. freshdesk** — no SDK
- **66. attio** — no SDK
- **68. apollo** — no SDK
- **73. social-scheduler** — wraps multiple commercial APIs
- **90. log-search** — multi-backend, no unified tool
- **94. quickbooks** — no full SDK
- **99. screenshot** — no dominant tool

**Total: 9 skills**

---

# Workflow

for every skill we must get a cli reference from the github repository which we must 100% replicate. So we will work spec driven.

Then we will create tests for those skills.

And only then we will implement.
