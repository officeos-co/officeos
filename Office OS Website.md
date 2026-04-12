---
category: Project
---

Struktur 1:1 basierend auf dem [[YC Website Blueprint]]. Validiert gegen Korso, Cignara, Kinro. Kontext aus [[Office OS]].

---

## 1. Navigation Bar

**Freiheit: Keine**

| Links                       | Rechts                     |
| --------------------------- | -------------------------- |
| Logo (Wortmarke, kein Icon) | "Start Free" (Primary CTA) |

Sonst nichts. Kein Pricing, kein Docs, kein Blog. Optional Login daneben wenn es Accounts gibt.

---

## 2. Hero Section

**Freiheit: Keine bei den Worten, etwas beim Visual**

**Headline:** "Stop managing agents manually. Deploy, connect, and control them from one place."

**Subtitle:** "Give every team their own AI agent with the right skills, permissions, and access to your organization's knowledge."

**Primary CTA:** "Start Free"
**Secondary CTA:** "Book a Demo"

**Visual:** Animierte Agent Deployment Sequenz — Agent wird deployed, verbindet sich zu GitHub + Notion + Slack, bekommt Permissions, startet zu arbeiten. 10s Loop. Fallback wenn Animation nicht gut genug: Echter Dashboard Screenshot mit laufenden Agenten aus verschiedenen Teams.

---

## 3. Social Proof Bar

**Freiheit: Niedrig — direkt nach dem Hero, nicht unten**

**Wenn Pilot-Partner vorhanden:**
"Trusted by" + 3-5 Logos in einer Row. Direkt unter dem Hero, noch vor den Features.

**Wenn noch keine Kunden aber Team-Credibility:**
"Built by engineers from" + relevante Logos (Unis, Unternehmen wo Founder gearbeitet haben). Kinro macht das mit DeepMind/Meta/Amazon — sehr effektiv.

**Wenn weder noch:**
Section weglassen. Kleines YC Badge im Hero reicht. Leerer Social Proof ist schlimmer als keiner.

---

## 4. Product in Action Section ⭐

**Freiheit: Hoch — hier wird Office OS einzigartig**

**Headline:** "One dashboard for every agent in your organization."
**Subtitle:** "Deploy agents per team. Each with their own skills, permissions, and knowledge graph access. Manage credentials, monitor activity, control everything centrally."

**Format:** Mehrere eigenstaendige Subsections (wie Cignara) statt Tabs. Jede Subsection zeigt einen konkreten Aspekt mit Text links + echtem UI Screenshot rechts:

**Subsection A: "Agent Deployment"**
Deploy a new agent in under a minute. Select a team, assign skills, set permissions — done. Runs in Kubernetes with minimal resources. No infrastructure work needed.
→ Visual: Screenshot vom Deploy-Flow (Team → Skills → Deploy → Running)

**Subsection B: "Custom Skills"**
Write your own skills in TypeScript. Deploy them to our cloud or run them in your local network — your agents execute real business logic on your infrastructure, not generic API calls through MCP. Skills run as V8 Isolates — sandboxed, fast, scalable.
→ Visual: Code Editor mit einem Skill Beispiel (z.B. ein ERP-Abgleich oder Invoice-Validierung) + daneben die Skill Library mit First-Party Skills (GitHub, Notion, Slack) und User-erstellten Custom Skills nebeneinander

**Subsection C: "Knowledge Graph"**
Every agent accesses your organization's knowledge. Per-team and per-organization graphs that stay in sync. No more agents that don't know what happened yesterday.
→ Visual: Screenshot oder interaktive Visualisierung des Knowledge Graph mit vernetzten Nodes

**Subsection D: "Central Credentials"**
API keys, tokens, service accounts — managed once, used by all agents. When a token expires, update it in one place. Not in a hundred agent configs.
→ Visual: Screenshot der Credentials-Verwaltung mit Status (active/expired)

---

## 5. Capability / Feature Sections ⭐

**Freiheit: Hoch — zweite Differenzierungszone**

Hier geht es um die Tiefe die Office OS von Alternativen unterscheidet. Mehrere Full-Width Sections:

**Section A: "Organization Knowledge Graph"**
Die grosse custom Visualisierung. Interaktiver Graph der zeigt wie Agenten, Teams, Tools und Wissen in einer Organisation zusammenhaengen. Das ist das Visual das Office OS von allem anderen unterscheidet — nicht ein weiteres Dashboard sondern eine lebendige Darstellung der Vernetzung.

**Section B: "Micro Footprint, Massive Scale"**
Konkrete Metriken wie Kinros "Applied Expertise" Grid:

- "100x less resources" — Hunderte Agenten wo andere Systeme 5 schaffen
- "<5ms cold start" — Skills als V8 Isolates, nicht als Container
- "Megabytes not gigabytes" — Rust-basierte Runtime, minimaler Footprint

**Section C: "Your logic. Your network. Not generic MCP."**
MCP servers call APIs. Your agents need to run actual business logic — validate invoices against your ERP, check compliance rules against internal policies, sync data between systems that don't have public APIs. Write it in TypeScript, deploy it as a skill, run it on your own network. Your agents do the work that no generic connector can automate.
→ Visual: Vergleich. Links: "Generic MCP" — simple API call, stateless, cloud-only. Rechts: "Custom Skill" — full TypeScript, access to local network, business-critical logic. Aehnlich wie Fed10s Comparison Section aber visueller.

---

## 6. Integration / Setup Section

**Freiheit: Niedrig**

**Headline:** "Connect your existing tools in minutes."
**Subtitle:** "Office OS sits on top of your current stack. No migration, no replacement."

**Integration Logos Row:** GitHub, Notion, Google Workspace, Slack, Linear, Jira

**Visual:** Dashboard Screenshot der verbundene Integrationen zeigt mit Status und Metriken.

---

## 7. Enterprise Trust / Security Section

**Freiheit: Niedrig**

**Headline:** "Infrastructure your IT team will trust."

- Kubernetes-native Deployment
- Sandboxed Skill Execution (V8 Isolates) — oder Self-Hosted Skills im eigenen Netzwerk
- Role-based Permissions per Agent
- Audit Logs fuer alle Agent-Aktionen
- Optional: SOC 2 / GDPR Badges wenn vorhanden

Kurz und sachlich. 3-4 Bullet Points oder Icons reichen.

---

## 8. Final CTA Section

**Freiheit: Mittel**

**Headline:** "Ready to deploy your first agent?"
**Subtitle:** "Start free with your own API keys. No credit card required."

**Zwei Optionen nebeneinander:**

- Links: "Start Free" Button → Self-serve Signup
- Rechts: Embedded Kalender Widget fuer "Book a Demo" (30 min, direkt auf der Seite wie Korso)

**Reassurance darunter:** "Free tier includes X agents. Bring your own API keys. Cancel anytime."

---

## 9. Footer

**Freiheit: Keine**

| Spalte 1       | Spalte 2                    | Spalte 3                 | Spalte 4                  |
| -------------- | --------------------------- | ------------------------ | ------------------------- |
| Logo + Tagline | **Product**: Features, Docs | **Company**: About, Blog | **Legal**: Privacy, Terms |

Social: GitHub, LinkedIn, X
"Made in Hamburg — © 2026 [Company Name] GmbH"

---

# Was NICHT auf die Website gehoert

- Kein Mission Statement
- Keine Vergleichstabelle (vs. Anthropic, vs. Openclaw, vs. SAP)
- Kein "How AI Agents Work" Explainer
- Keine Roadmap / Coming Soon Features
- Keine Pricing Page bis echte Tiers existieren
- Kein Scroll Hijacking
- Keine Stock Photos, keine generischen Icons
- Keine Purple Gradients

# Design

- Dark mode — passt zum Developer/Ops Kontext
- System Font oder eine einzige Schrift
- Ein Accent Color (electric blue oder teal) fuer CTAs
- Desktop-first, responsive
- Static HTML + minimales JS
- Body Text links-buendig, nie zentriert (ausser Headlines)
- Zeilenlaenge 45-90 Zeichen
- Keine thin fonts ausser in Headlines
