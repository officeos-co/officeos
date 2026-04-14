import Link from "next/link";
import { ArrowLeft } from "lucide-react";

export const metadata = {
  title: "Privacy Policy — Office OS",
  description: "How Office OS collects, processes, and protects your data.",
};

export default function Privacy() {
  return (
    <div className="min-h-screen bg-background text-primary font-sans">
      <nav className="sticky top-0 z-50 border-b border-border bg-background/80 backdrop-blur-md">
        <div className="mx-auto flex max-w-4xl items-center justify-between px-6 py-4">
          <Link
            href="/"
            className="flex items-center gap-2 text-sm text-muted-foreground hover:text-primary transition-colors"
          >
            <ArrowLeft className="h-4 w-4" />
            Back
          </Link>
          <span className="font-medium tracking-tight">Office OS</span>
          <div className="w-16" />
        </div>
      </nav>

      <main className="mx-auto max-w-4xl px-6 pt-20 pb-28 md:pt-28">
        <h1 className="text-4xl font-bold tracking-tight text-center md:text-5xl">
          Privacy Policy
        </h1>
        <p className="mt-4 text-sm text-muted-foreground text-center">
          Last updated: April 13, 2026
        </p>

        <div className="mt-16 space-y-12 text-muted-foreground leading-relaxed">
          {/* Intro */}
          <section>
            <p>
              Office OS GmbH (&quot;we&quot;, &quot;us&quot;, &quot;our&quot;)
              operates the Office OS platform at{" "}
              <strong className="text-primary">officeos.co</strong>. This
              Privacy Policy explains what data we collect, how we process it,
              and your rights regarding that data.
            </p>
          </section>

          {/* Data We Collect */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              1. Data We Collect
            </h2>
            <ul className="space-y-3 list-disc pl-5">
              <li>
                <strong className="text-primary">Account information</strong> —
                email address, name, and authentication credentials when you
                sign up.
              </li>
              <li>
                <strong className="text-primary">Agent data</strong> —
                personality files, workspace content, and configuration stored
                in per-agent CouchDB vaults.
              </li>
              <li>
                <strong className="text-primary">LLM call logs</strong> —
                prompts and completions routed through our backend proxy are
                logged for debugging and audit purposes.
              </li>
              <li>
                <strong className="text-primary">Credential metadata</strong> —
                API keys and tokens you provide for third-party services,
                encrypted at rest in Postgres.
              </li>
              <li>
                <strong className="text-primary">Browser session data</strong> —
                cookies and session state stored per-agent in Postgres for the
                browser skill.
              </li>
              <li>
                <strong className="text-primary">Usage data</strong> — skill
                execution logs, tool call history, and agent activity metrics.
              </li>
            </ul>
          </section>

          {/* How Data Is Processed */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              2. How Data Is Processed
            </h2>
            <p>
              All LLM calls from your agents are proxied through our backend.
              Your prompts and completions are forwarded to third-party LLM
              providers (OpenAI, Anthropic, Google, xAI, Groq, DeepSeek,
              OpenRouter, or Ollama) depending on your configuration. We do not
              modify the content of these calls beyond injecting the required
              authentication headers.
            </p>
            <p className="mt-3">
              Skills are executed in a sandboxed Node.js runtime. Skill inputs
              and outputs pass through the backend, which logs them for audit
              and debugging.
            </p>
          </section>

          {/* Credential Storage */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              3. Credential Storage
            </h2>
            <p>
              All credentials (API keys, tokens, service account keys) are
              encrypted at rest in our Postgres database. Agent pods never
              receive raw credentials — the backend injects them per-request
              through a secure proxy. Credentials are never exposed in logs,
              environment variables, or agent workspaces.
            </p>
          </section>

          {/* Browser Sessions */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              4. Browser Session Data
            </h2>
            <p>
              When agents use the browser skill, session cookies are persisted
              in Postgres per-agent. Agents themselves are session-unaware —
              session management is handled transparently by the backend.
              Browser session data is deleted when the associated agent is
              deleted.
            </p>
          </section>

          {/* No Data Selling */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              5. No Data Selling
            </h2>
            <p>
              We do not sell, rent, or share your personal data or agent data
              with third parties for marketing or advertising purposes. Data is
              only shared with third-party LLM providers as necessary to fulfill
              your agent requests, and only when you have configured those
              providers.
            </p>
          </section>

          {/* GDPR */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              6. GDPR Compliance
            </h2>
            <p>
              If you are in the European Economic Area, you have rights under
              the General Data Protection Regulation (GDPR), including the right
              to access, rectify, erase, and port your data.
            </p>
            <ul className="mt-3 space-y-3 list-disc pl-5">
              <li>
                <strong className="text-primary">
                  Data portability (Art. 20)
                </strong>{" "}
                — our agent runtime (zeroclaw-core) includes a built-in{" "}
                <code className="rounded bg-muted px-1.5 py-0.5 text-sm font-mono">
                  memory_export
                </code>{" "}
                tool that lets you export all agent memory and workspace data in
                a machine-readable format.
              </li>
              <li>
                <strong className="text-primary">Right to erasure</strong> — you
                can delete your account and all associated agent data at any
                time through the dashboard or by contacting us.
              </li>
              <li>
                <strong className="text-primary">Data processing basis</strong>{" "}
                — we process your data based on contractual necessity (to
                provide the service) and your consent where required.
              </li>
            </ul>
          </section>

          {/* Data Retention */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              7. Data Retention
            </h2>
            <p>
              We retain your data for as long as your account is active. When
              you delete an agent, its CouchDB vault, browser sessions, and
              execution logs are permanently removed within 30 days. When you
              delete your account, all associated data is permanently deleted
              within 30 days. LLM call logs are retained for 90 days for
              debugging purposes, then automatically purged.
            </p>
          </section>

          {/* Security */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              8. Security
            </h2>
            <p>
              We use industry-standard measures to protect your data: TLS in
              transit, encryption at rest for credentials and sensitive data,
              role-based access controls, and isolated Kubernetes pods per
              agent. All infrastructure runs in a single-tenant, self-hosted
              architecture.
            </p>
          </section>

          {/* Contact */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              9. Contact
            </h2>
            <p>
              For privacy concerns, data access requests, or questions about
              this policy, contact us at{" "}
              <a
                href="mailto:privacy@officeos.co"
                className="text-primary underline underline-offset-4 hover:text-primary/80"
              >
                privacy@officeos.co
              </a>
              .
            </p>
          </section>
        </div>
      </main>

      <footer className="border-t border-border py-8 text-center text-sm text-muted-foreground">
        Made in Hamburg — &copy; 2026 Office OS GmbH
      </footer>
    </div>
  );
}
