import Link from "next/link";
import { Navbar } from "@/components/sections/navbar";
import {
  Plug,
  Globe,
  MessageSquare,
  Terminal,
  Server,
  Workflow,
} from "lucide-react";

export const metadata = {
  title: "Deep Integrations — Office OS",
  description:
    "Real API integrations that work like native tools. GitHub, Notion, Google, Slack, browser automation, self-hosted runners, and a unified GraphQL skill interface.",
};

export default function IntegrationsPage() {
  return (
    <div className="min-h-screen bg-background text-primary font-sans">
      <Navbar />

      <main className="mx-auto max-w-4xl px-6 pt-20 pb-28 md:pt-28">
        <h1 className="text-4xl font-bold tracking-tight text-center md:text-5xl">
          Deep Integrations
        </h1>
        <p className="mt-4 text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed text-center">
          Not generic MCP servers or thin wrappers. Real API integrations that
          work like native tools — with central credential management, per-agent
          sessions, and self-hosted runners for on-premise systems.
        </p>

        <div className="mt-16 space-y-12 text-muted-foreground leading-relaxed">
          {/* First-Party Skills */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Plug className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                First-Party Integrations
              </h2>
            </div>
            <p>
              Office OS ships with a growing set of first-party skills built by
              the core team:{" "}
              <strong className="text-primary">
                GitHub, Notion, Google, and Obsidian
              </strong>
              . These aren&apos;t generic API clients — they&apos;re
              purpose-built integrations that expose the actions agents actually
              need: creating issues, querying databases, managing documents, and
              searching knowledge bases.
            </p>
            <p className="mt-4">
              Every first-party skill is built with the same{" "}
              <strong className="text-primary">@harro/skill-sdk</strong> that
              you use for custom skills. There&apos;s no privileged internal
              API, no special treatment. First-party skills are simply
              well-tested, well-maintained packages that ship with the platform.
            </p>
          </section>

          {/* Browser Skill */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Globe className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Browser Automation
              </h2>
            </div>
            <p>
              The browser skill uses{" "}
              <strong className="text-primary">Playwright</strong> to give
              agents headless Chrome control — navigating pages, filling forms,
              clicking buttons, extracting content, and taking screenshots.
              It&apos;s a system skill, meaning it&apos;s always available
              without manual installation or credential configuration.
            </p>
            <p className="mt-4">
              Each agent gets{" "}
              <strong className="text-primary">
                per-agent session persistence
              </strong>
              . The backend transparently manages browser sessions and persists
              cookies in Postgres. Agents are session-unaware — they just browse
              the web, and the platform handles login state, session cookies,
              and context across interactions. An agent can log into a service
              once and maintain that session across conversations.
            </p>
          </section>

          {/* Channel Integration */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <MessageSquare className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Channel Integration
              </h2>
            </div>
            <p>
              Agents communicate through the channels your team already uses:{" "}
              <strong className="text-primary">
                Slack, Email, and WhatsApp
              </strong>
              . Instead of requiring users to switch to a new interface, agents
              meet your team where they work.
            </p>
            <p className="mt-4">
              A support agent can respond to customer emails, a project manager
              agent can post updates in Slack channels, and a notification agent
              can send WhatsApp alerts. Channel integration is bidirectional —
              agents can both send and receive messages, enabling natural
              conversational workflows without the dashboard.
            </p>
          </section>

          {/* Unified Interface */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Terminal className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Unified Skill Interface
              </h2>
            </div>
            <p>
              Agents call every integration through a single{" "}
              <strong className="text-primary">skill_exec</strong> tool that
              presents a CLI-like interface over GraphQL. Whether it&apos;s
              creating a GitHub issue, querying a Notion database, or browsing a
              webpage, the interface is the same: skill name, action name,
              parameters.
            </p>
            <p className="mt-4">
              This unified interface means the LLM doesn&apos;t need to learn
              different tool formats for different integrations. Every skill
              looks the same to the agent, reducing prompt complexity and
              improving tool selection accuracy. Every integration also benefits
              from central credential management — API keys stored once, used
              everywhere.
            </p>
          </section>

          {/* Self-Hosted Runners */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Server className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Self-Hosted Runners
              </h2>
            </div>
            <p>
              For integrations that need to access on-premise systems, Office OS
              provides{" "}
              <strong className="text-primary">self-hosted runners</strong> —
              Docker containers that run inside your network and poll the
              backend for jobs. No tunnels, no firewall changes, no inbound
              ports required.
            </p>
            <p className="mt-4">
              Runner authentication uses{" "}
              <strong className="text-primary">
                RFC 8628 device authorization flow
              </strong>{" "}
              — the same pattern used by GitHub CLI and Docker Desktop. You
              start the runner, approve it through a browser-based flow, and
              it&apos;s connected. Skills sync automatically to runners, and
              custom skills uploaded through the dashboard are pulled and
              available on the runner without manual intervention.
            </p>
          </section>

          {/* Extensibility */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Workflow className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Universal Extensibility
              </h2>
            </div>
            <p>
              Any software that exposes a{" "}
              <strong className="text-primary">GraphQL or REST API</strong> can
              be integrated as a skill. The SDK provides a straightforward
              pattern: define your actions, declare input/output schemas with
              Zod, specify required credentials, and publish. The platform
              handles discovery, credential injection, and execution.
            </p>
            <p className="mt-4">
              This is the fundamental architectural advantage over managed agent
              platforms that are limited to MCP server access. Office OS agents
              have <strong className="text-primary">full system control</strong>{" "}
              — they can interact with any API, any internal service, any tool
              that your organization runs. The integration surface is bounded
              only by what you choose to expose as skills.
            </p>
          </section>
        </div>

        {/* CTA */}
        <div className="mt-24 text-center">
          <h2 className="text-2xl font-bold tracking-tight mb-4">
            Connect your tools
          </h2>
          <div className="flex flex-row items-center justify-center gap-3">
            <Link
              href="https://dashboard.officeos.co"
              className="flex h-9 items-center justify-center whitespace-nowrap rounded-full bg-secondary px-6 text-sm font-normal text-primary-foreground tracking-wide shadow-sm transition-all hover:bg-secondary/80 active:scale-95"
            >
              Start Free
            </Link>
            <Link
              href="/"
              className="flex h-9 items-center justify-center whitespace-nowrap rounded-full border border-border px-6 text-sm font-normal tracking-wide transition-all hover:bg-muted active:scale-95"
            >
              Back to Home
            </Link>
          </div>
        </div>
      </main>

      <footer className="border-t border-border py-8 text-center text-sm text-muted-foreground">
        Made in Hamburg — © 2026 Office OS GmbH
      </footer>
    </div>
  );
}
