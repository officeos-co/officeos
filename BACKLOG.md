# BACKLOG

---

## CONCLUDED — Active Decisions

---

### Agent Panel UI / Sidebar Restructure / Skills → Tools

Make agent panel less tab heavy. Claude also does have managed agents it also has the same functional sidebar layout and it shows that you can put all the menaingful information on one page.

Claude only has one prompt, mcp tools but it shows how to abstract tools well. Basically the abstraction is that you dont have to separarte tools and skills. Also skills i the industry has been established as just knoweldge.
Our abstraction for a skill hub is thus incorrect we should reframe them as tools. Thus The agent detaiils should follow claudes layout. I would probably propose one agent tab which includes a chat -> system prompt -> tools

The second tab sohuld be sessions. Then we would want a logs tab. And a memory tab which also isnt perfect currently. The problem with system prompt in claude agents is that it is really just a single prompt. In our implementation its made up of several files. We would need to brainstorm about that. But id say openclaw established that a system prompt made out of those files is good. WE shouldnt reduce that. So id probably propose just put Prompt into a separate tab since its complex. And separate memory from prompt although both should be stored in obsidian.

Also providers really shouldnt be a sidebar element. Claude code has a really cool sidebar animation if switching to organizational settings it changes the whole sidebar into a new view. This transition is really good. The image shows that the sidebar then basically has a back to main app button we should implement it exactly like that. Because there are system administrators which need to do heavy config. This is the perfect ui layout for the coming complexity regarding enterprise. Where stuff like privacy controls, rate limiting, team setup and so on would be configured from there.

Also remove new agent from sidebar.

The philosophy is Main app is for the consumer — acceptance criteria is even a non technical person should instantly understand whats going on and not have to think about any config or permission they should just be subject to it.

\*Conclusion after talking:

**Sidebar restructure:**

- Remove Platform section (Providers, Skills, Runners) from main sidebar entirely → move to Org Settings
- Main sidebar: Dashboard, Agents, [future: Knowledge Graph], then bottom: Org Settings + Account/Logout
- Org Settings gets the Claude Code sidebar transition — clicking it replaces the full sidebar with an org-settings view that has a "Back to main app" button at the top. This is the right pattern for the coming enterprise complexity (privacy controls, rate limiting, team setup, API keys, runners config).
- "Active Agents" section stays in sidebar as-is (it's operational, not config)
- Agent creation lives as a big button inside the Agents tab — confirmed correct, do not add it back to the sidebar

**Rename Skills → Tools everywhere** (skills = knowledge in the industry; tools = callable capabilities — our abstraction is wrong, fix it)

**Agent detail panel — 5 tabs:**

1. **Chat + Tools** — operational interface, chat window + assigned tools list with emoji icons
2. **Prompt** — separate tab because our system prompt is a vault of files (CouchDB/Obsidian), not a single text field; cannot collapse this
3. **Sessions** — session history
4. **Logs** — tool execution logs
5. **Memory** — what the agent has learned; separate from Prompt even though both live in Obsidian (different concerns: behavior vs. learned context)

**Icons:** Keep emojis for now, they work and are distinctive. Revisit when there is a clear reason to upgrade.

---

### Obsidian / Knowledge Graph

We want to use obsidian as the only source for knoweldge. I know that partially it has already been established that at least the system prompt is pulled from couchdb. But we need to provide the agent with a meaningful way of interacting with the organizations knowledge graph.

We have built the skill for it /Users/harrokrog/Desktop/EnterpriseAgentOs/packages/skills/obsidian which is the skill to interact with the knowledge graph trough the cli. But maybe we would need to make it actually native tools. But the cli is so komplex and we need that complexity to all persist.

This org wide knoweldge graph also needs to be a new tab in the sidebar.

\*Solution after talking:

**Agent ↔ knowledge graph access (already solved):** The obsidian skill already exposes discrete, typed tools (read_note, search, get_backlinks, find_by_tag, etc.) via the skill SDK. obsctl is just the execution engine — the agent never sees raw CLI. No architecture change needed here.

**Human ↔ knowledge graph (sidebar tab MVP):** Add a Knowledge Graph sidebar section. MVP scope: file tree + full-text search + markdown preview. Graph visualization (nodes/edges) is v2. The CouchDB connection for this tab is an org-level config (org settings), not per-agent.

**System prompt — continuous memory updates:** Agent system prompts must explicitly instruct agents to treat the knowledge graph as their external memory and update it continuously. Guided by the philosophy in `/Users/harrokrog/Documents/Optimierung/README.md`:

- After completing tasks, the agent appends findings to existing relevant notes before creating new ones (text-dump-first: only atomize when a note exceeds ~500 lines or is referenced from many places)
- When creating a note, always set `category` (one per note, defines identity) and `tags` (sparse, multi-dimensional, only when a concept genuinely spans categories)
- Use the org's template per category when creating structured notes (`apply_template`)
- Link notes using `[[wikilinks]]` in content to build the graph — the graph is the structure, not the folder hierarchy
- Folders are architecture only; agents should file notes by the org's folder convention, not invent new folders

**Tool gaps to fix — tags, categories, bases must work:**

- Tags: `find_by_tag` exists ✅
- Categories: `get_properties` / `set_property` can read/write a `category` frontmatter field, but there is no `find_by_category` action — **add `find_by_category` to the obsidian skill**
- Bases: Obsidian Bases are filtered views of notes by property. obsctl likely doesn't support this yet — **add bases/filtered-property-query support to obsctl and expose it as a tool**

---

### Enterprise Auth

I really dont know if enterprise auth is any different but right now there is no way to join enterprises we would need to talk about that. Probably we would need to support more auth methodes such as github, microsoft, iam from aws and so on.

\*Solution after talking:

**Standard: OIDC (OpenID Connect) + SAML 2.0**

- OIDC is the primary protocol — covers Google, GitHub, Microsoft (Entra ID), AWS IAM Identity Center, all modern IdPs
- SAML 2.0 is required for large enterprises on Okta, Azure AD legacy, OneLogin, PingIdentity — a hard blocker without it

**Implementation: WorkOS** (not DIY)

- Single API abstracting OIDC, SAML 2.0, and SCIM (directory sync)
- Per-org connections: each enterprise customer configures their own IdP once, all employees SSO automatically
- C# backend integrates via WorkOS SDK — no hand-rolled SAML state machines

**SCIM (user provisioning) is mandatory for enterprise:** when an employee leaves, their agent access must auto-revoke. This is a compliance requirement, not a nice-to-have.

---

### Coding Agent

I am currently thinking about building a coding agent which integrates with the network. This agent would have access to our knowledge graph. But really this can be pushed long into the future because right now no one really does coding agents on api billing.

But we would need to add a really mature coding agent skill. Basically any coding agent would clone a repository and should work on it in a isolated enviroment. We can again inject api keys pretty easily and it should "just work". I am currently questioning if a coding agent is so custom that it should not just be a npm package.

\*Solution after talking:

**Don't build a coding agent from scratch — fork opencode.**

opencode already solves the hard problems: file editing, diff application, shell execution, multi-turn coding loop. Our contribution is:

- Strip the TUI entirely — no interactive terminal needed, headless only
- Inject the LLM provider via our backend (consistent with "credentials never leave the backend" — the fork receives a provider config, not raw API keys)
- Run in an isolated environment using **git worktree** — fast (same object store, no clone), isolated per task, own branch, clean to remove after
- Called as an async tool by the orchestrator agent — main agent fires the task, coding agent works independently, returns result (branch name, PR link, summary)

**Why not a subagent within the same agent, or a native Rust implementation:**

- Competing with opencode/Claude Code on coding UX is the wrong move — users have strong tool preferences
- Our position is orchestration and org management, not coding execution
- opencode's engine is the right foundation; we just make it programmable and headless
- Coding agents are fundamentally seemingly the same but trying to balance zeroclaw fork being a coding agent and a orchestrator at the same time would be too complex

**Delay:** This is a real engineering effort. Ship everything else first. Revisit when there is a concrete customer need for it.

---

### Own Model / Stripe / Pricing

Like opencode we could implement a model router so users dont have to bring their own keys. But most importantly we should implement model forwarding so that we basically resell the models.

This goes right along with stripe. Since we are b2b idk if a simple SAAS pricing is good but i guess so. We should integrate stripe probably having 3 tiers where tier 3 would be a custom contact us enterprise tier. We should consider pricing structure and to what way we wanna integrate normal users.

\*Solution after talking:

**Model strategy: resell models, no BYOK in SaaS** (BYOK is a future self-hosted concern, not in scope now)

**Smart model routing is what makes the economics work** — not every call goes to Sonnet. Haiku handles simple tool calls and routing decisions, Sonnet handles actual reasoning. Real blended cost lands at ~$3-5/M tokens across a typical workload. This routing is itself a product feature.

**Model cost reference (retail, pre-volume-discount):**

- Haiku: ~$0.50/M blended
- Sonnet: ~$9/M blended
- Opus: ~$45/M blended

**Typical agent usage:**

- Light agent: ~1M tokens/month
- Active agent: ~3M tokens/month
- Heavy agent: ~8M tokens/month

**3 tiers — limits on concurrent agents + token bundle, all features available at every tier:**

|                   | Free       | Team        | Enterprise |
| ----------------- | ---------- | ----------- | ---------- |
| Price             | $0         | $249/mo     | Custom     |
| Concurrent agents | 1          | 10          | Custom     |
| Tokens included   | 2M/mo      | 25M/mo      | Custom     |
| Our model cost    | ~$10       | ~$100       | —          |
| Gross margin      | −$10 (CAC) | ~$149 / 60% | target 65% |

Enterprise starting point: ~$1,500–3,000/month. Justified by SSO (WorkOS), SCIM, compliance, and dedicated support — all of which have real cost to serve.

**On power users:** Heavy agents at full Sonnet load can flip a Team subscription to a loss (25M tokens at Sonnet = $225 in model costs vs $249 revenue). This is expected — every company loses money on power users (AWS, Notion, Figma all do). We accept this as normal. The pay-as-you-go model ensures we never run at sustained loss on a single customer.

**Profitability path is not only token price drops:** Revenue grows as we add customers, model routing improves, volume discounts kick in, and the platform expands (coding agents, channel integrations, marketplace). The business has multiple expansion levers beyond model cost reduction.

**Pay-as-you-go after threshold (Cursor model):** Every plan includes a token bundle, after which on-demand usage kicks in automatically, billed in arrears. No hard cutoff that breaks an agent mid-task. Users see their usage dashboard and get notified as they approach limits.

- Overage rate: our model cost + markup (e.g., cost × 1.3) — we never lose money on overage
- Enterprise gets pooled token budgets across all agents + invoice/PO billing instead of card

**Implementation:** Stripe for subscriptions + Stripe usage-based billing (metered) for token overage. Add a pricing page.

---

### Landing Page / Product Video / Custom Assets

Our landing page right now just follows the template. But most importantly we need to have a really good product video and custom elements for the how our product works and for the feature section.

We really wanna respect the yc blueprint /Users/harrokrog/Documents/Optimierung/References/YC Website Blueprint.md — right now we have too much features and too little showcase. For verification another agent should apply those 50 principles critically /Users/harrokrog/Documents/Optimierung/Clipping/YC Landing Page Teardown 50 Lessons.md.

We dont need to resell what Openclaw is. Its common knoweldge as of now. So stuff like personalized prompt or persistent memory wont hit at all.

\*Conclusion after talking:

**Headline: "The AI workforce for your company"**
This is the red line throughout the entire website. Every section reinforces this framing — agents are workers, not tools. They slot into your org like employees do, but handle the structured repetitive work so your people focus on what makes them unique.

**Page structure (3 sections after hero):**

1. **Product in Action** — the most important investment
   - A high-quality screen recording of a real agent completing a real task is the primary asset. No stock graphics, no image generation.
   - Custom graphics to support the video — animated sequence showing the agent being deployed and doing work inside an org
   - No no-code diagram tools or Canva-style output — React/Lottie/CSS animations only

2. **Features** — show don't list, each with a custom visual:
   - Company-wide knowledge graph — animated node graph, most visually unique differentiator
   - Deep integrations — tool icons connecting, not a bullet list (100+ tools angle)
   - Custom skills — show the skill SDK / one-click deploy
   - Quick deployment — agent up in seconds, Kubernetes under the hood
   - Rust-based runtime — signal to technical buyers that this is fast and stable

3. **Enterprise Trust** — already a strong section, keep it focused:
   - Sandboxing (skills run in isolated V8 isolates)
   - Security (credentials never leave the backend)
   - Central logging (every tool call logged)
   - Self-hosted runners (skills run on your infra)
   - Guardrails (rate limiting, permission model)

**What to drop from the landing page:** persistent memory, personalized prompts (openclaw baseline), computer use relay (too early), browser relay (too early), "decoupled tools" framing (too technical to mean anything to a buyer).

---

---

## MUST SHIP BEFORE LAUNCH

---

### Browser

This is way higher priority. We basically cant sell our product if we dont have a browser working. The current state of the art is that any agent has their browser internally. But obviously i wanna decouple the browser too. We would for sure need to persist session cookies and so on. Also we cant get banned. But i actually have a library for that. Also i think openclaws using the browser never really gets banned. They just use playwright. There is also a library for making playwright more human.

**Decision:** Playwright-based browser skill. Stealth plugin for anti-ban. Session cookies persisted in the backend (not in the agent pod). Decoupled like all other skills — the agent calls browser tools, the skill-runtime executes Playwright. The user's existing anti-ban library is the right foundation.

---

### Agent in Any Channel Integration

That is like a main feature of openclaw and right now we dont have it. The agent obviously should integrate to any channel like microsoft teams, whatsapp, telegram.

**Decision:** Slack and Teams first (enterprise priority), WhatsApp and Telegram later. Architecture: webhook per channel → backend routes message to agent's chat gateway → agent responds → backend sends back to channel. Each channel is a connector configured in org settings. The agent doesn't know or care which channel it's talking to.

---

### Privacy Policy and Terms of Service

The terms of service and privacy policy should really make us not accountable for anything failing. But since we are doing enterprise maybe we should consider taking some accountability. Usually standard SaaS has limitation of liability, individual enterprise contracts then add SLAs on top.

**Decision:** Use Termly or similar to generate the base, customize liability clauses. Standard SaaS limitation of liability in the public terms. Enterprise SLAs negotiated per contract. Ship this before any enterprise outreach.

---

---

## HIGH VALUE — DO EARLY

---

### Downtime View / Status Page

To prove that they can trust our infrastructure we should show a state of the art downtime panel referenced in the footer.

**Decision:** Betterstack or Instatus. 15 minutes to set up, huge enterprise trust signal. Reference in footer. Do this before any enterprise outreach.

---

### Publishing Skills Publicly

Since we are b2b the default should be private within the organization but we should encourage people to publish their tools. Like clawhub. So that our skill network grows.

**Decision:** Private by default within org is correct. Design the skill system with public publishing in mind now, but don't build the marketplace UI until there are 100+ first-party skills and real third-party interest. Network effect play — the more skills exist publicly, the stronger the platform.

---

### MCP Server for Access

Generally we want to provide access not only through the dashboard but also through a cli and mcp server. Example https://re-entry.ai/features/mcp-gateway

**Decision:** MCP server makes the platform compatible with Claude Code, Cursor, and the broader ecosystem. Agents become accessible as MCP tools from any MCP-compatible client. Medium effort, strong developer credibility, good for YC story. CLI access comes after MCP.

---

### Agent Skill Assignment

Pretty easy — right now all skills are always attached to every agent. We would want to at setup decide which skills we want, or later manually changing them.

**Decision:** Straightforward feature. At agent creation, choose which tools to attach. Editable later from the Chat + Tools tab. Low effort, high value for permission control and keeping agent context clean.

---

### Loading Placeholder Skeletons

Just add meaningful skeleton placeholders. Especially an anti pattern is that we sometimes show wrong default values instead of waiting for the correct values. Add meaningful error popups if something goes wrong. Especially in enterprise rather show something went wrong than showing something wrongly.

**Decision:** Replace all default value placeholders with skeletons. Add error boundaries with meaningful messages throughout the dashboard. Enterprise principle: honest failure > misleading state.

---

---

## DELAY

---

### Coding Agent (opencode fork)

Concluded above — delay until there is concrete customer demand. The architecture is decided (headless opencode fork, git worktree, backend-injected provider), just not the right time to build it.

---

### Computer Use Relay

Claude computer use already exists and with a custom docker container you can actually allow it to control your computer. We basically wanna clone that software and make it work in the cloud for us. And most importantly through relays. The user would need to install part of the software. There must be a 1 to 1 assignment of agents to a relay because the agent needs to know about the computer a lot and that context would pollute other agents.

Dashboard placement: not in tools. Under the agents tab as a special agent type with a live window showing what the agent is doing.

Flow: intent → agent does its thing async → sends response or meaningful error.

**Decision:** Architecture is clear, timing is wrong. Nobody is buying this yet. Revisit when there is real customer demand.

---

### Agent Visualization

It would be cool to see dependencies between agents and skills. Like for paperclip which shows the org structure.

**Decision:** Delay. Interesting but not a blocker. Revisit when there are enough agents in a deployment to make the visualization meaningful.

---

### SDK in Any Language (Extism / WASM)

Extism (extism.org) — WASM-based plugin system. Developers write skills in any language (Rust, Go, Python, JS, C), compile to .wasm, and the runtime loads them into a single process. No containers per skill, no build pipelines. The host provides host functions for HTTP, credentials, logging — which maps directly to SkillContext.

No architectural trade-off, no new services. It's a pure upgrade path. The only cost is time — weeks of work to replace something that works fine today for a capability nobody is asking for yet.

**Decision:** Don't do it yet. Pure upgrade path when there is demand for non-TypeScript skills.

---

### Relay Only Skills (OS Specific)

For the apple notes skill that skill would actually work if the user installed a relay on their device. The agent could then execute the skill on their system. see https://github.com/antoniorodr/memo

**Decision:** Interesting concept, too niche. Delay until there is real customer demand for OS-specific skills.

---

### CLI for Access

Developer CLI for scripting, CI/CD integration, programmatic access.

**Decision:** After MCP server is shipped. MCP covers the ecosystem compatibility angle first.

---

### Blog

Don't want to write meaningless content. Two posts max when the time is right:
1. "Why openclaw isn't enterprise ready" — casual, direct, establishes positioning
2. Winning the disability hackathon — social proof even if unrelated to the current project

**Decision:** Not urgent. Write when there is something real to say.

---

---

## SKIP

---

### Proxy Network

Seems pretty shady. Not aligned with the enterprise trust positioning. Skip entirely.

---
