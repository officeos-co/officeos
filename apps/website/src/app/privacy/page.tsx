import Link from "next/link";
import { Navbar } from "@/components/sections/navbar";
import { FooterSection } from "@/components/sections/footer-section";

export const metadata = {
  title: "Privacy Policy — OfficeOS",
  description: "How OfficeOS collects, processes, and protects your data.",
};

export default function Privacy() {
  return (
    <div className="relative mx-auto max-w-7xl border-x">
      <div className="absolute top-0 left-6 z-10 block h-full w-px border-border border-l" />
      <div className="absolute top-0 right-6 z-10 block h-full w-px border-border border-r" />
      <Navbar />

      <main className="mx-auto max-w-4xl px-6 pt-20 pb-28 md:pt-28">
        <h1 className="text-4xl font-bold tracking-tight text-center md:text-5xl">
          Privacy Policy
        </h1>
        <p className="mt-4 text-sm text-muted-foreground text-center">
          Last updated: April 15, 2026
        </p>

        <div className="mt-16 space-y-12 text-muted-foreground leading-relaxed">
          {/* Intro */}
          <section>
            <p>
              OfficeOS, operated by Harro Krog (Einzelunternehmen, Hamburg,
              Germany) (&quot;we&quot;, &quot;us&quot;, &quot;our&quot;),
              operates the OfficeOS platform at{" "}
              <strong className="text-primary">officeos.co</strong>. This
              Privacy Policy explains what data we collect, how we process it,
              and your rights regarding that data.
            </p>
          </section>

          {/* Data Controller vs Processor */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              1. Data Controller and Data Processor
            </h2>
            <p>
              OfficeOS acts as a{" "}
              <strong className="text-primary">data processor</strong> when
              handling data that your agents process on your behalf. In this
              capacity, you are the data controller and are responsible for
              ensuring that your use of agents complies with applicable data
              protection laws. We act as a{" "}
              <strong className="text-primary">data controller</strong> only for
              account information you provide directly to us (e.g., your email
              address, name, and billing information).
            </p>
            <p className="mt-3">
              This distinction means that we are not responsible for the
              personal data your agents collect, process, or generate on your
              behalf. You must ensure that you have a lawful basis for any
              personal data processed through your agents.
            </p>
          </section>

          {/* Data We Collect */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              2. Data We Collect
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
              3. How Data Is Processed
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

          {/* Third-Party Data Sharing */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              4. Third-Party Data Sharing and Sub-Processors
            </h2>
            <p>
              When your agents interact with third-party services (LLM
              providers, APIs, websites), data is transmitted directly to those
              third parties according to your agent configuration.{" "}
              <strong className="text-primary">
                By configuring your agents to use third-party services, you
                acknowledge that data is shared with those third parties and is
                subject to their respective privacy policies, not ours.
              </strong>
            </p>
            <p className="mt-3">
              Our current sub-processors include: cloud infrastructure providers
              for hosting, LLM providers as configured by you, and payment
              processors for billing. A current list of sub-processors is
              available upon request.
            </p>
          </section>

          {/* Credential Storage */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              5. Credential Storage
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
              6. Browser Session Data
            </h2>
            <p>
              When agents use the browser skill, session cookies are persisted
              in Postgres per-agent. Agents themselves are session-unaware —
              session management is handled transparently by the backend.
              Browser session data is deleted when the associated agent is
              deleted.
            </p>
          </section>

          {/* AI Output Accuracy */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              7. AI Output Accuracy and Personal Data
            </h2>
            <p>
              Agent outputs are generated by third-party large language models.
              We cannot guarantee the factual accuracy of any outputs, including
              outputs that may contain personal data. Due to the technical
              complexity of these models, it may not always be possible to
              identify, correct, or delete specific personal data within
              generated outputs.
            </p>
            <p className="mt-3">
              You should not rely on agent outputs as a sole source of factual
              information, particularly regarding personal data of third
              parties. You are responsible for verifying the accuracy of all
              agent outputs before acting on them or sharing them.
            </p>
          </section>

          {/* No Data Selling */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              8. No Data Selling
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
              9. GDPR Compliance
            </h2>
            <p>
              If you are in the European Economic Area, you have rights under
              the General Data Protection Regulation (GDPR), including the right
              to access, rectify, erase, and port your data. These rights are
              not absolute and may be subject to limitations where we have a
              lawful basis for continued processing.
            </p>
            <ul className="mt-3 space-y-3 list-disc pl-5">
              <li>
                <strong className="text-primary">
                  Data portability (Art. 20)
                </strong>{" "}
                — our agent runtime includes a built-in{" "}
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
              <li>
                <strong className="text-primary">Right to object</strong> — you
                may object to processing of your data at any time. We will cease
                processing unless we have compelling legitimate grounds.
              </li>
            </ul>
          </section>

          {/* Data Retention */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              10. Data Retention
            </h2>
            <p>
              We retain your data for as long as your account is active or as
              needed to provide the service. When you delete an agent, its
              CouchDB vault, browser sessions, and execution logs are
              permanently removed within 30 days. When you delete your account,
              all associated data is permanently deleted within 30 days.
            </p>
            <p className="mt-3">
              LLM call logs are retained for 90 days for debugging purposes,
              then automatically purged. We may retain certain data beyond these
              periods where required by law, for the establishment or defense of
              legal claims, or for legitimate safety and fraud prevention
              purposes.
            </p>
          </section>

          {/* Security */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              11. Security
            </h2>
            <p>
              We use industry-standard measures to protect your data: TLS in
              transit, encryption at rest for credentials and sensitive data,
              role-based access controls, and isolated Kubernetes pods per
              agent. While we implement reasonable security measures, no method
              of electronic transmission or storage is 100% secure, and we
              cannot guarantee absolute security.
            </p>
          </section>

          {/* Changes */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              12. Changes to This Policy
            </h2>
            <p>
              We may update this Privacy Policy from time to time. Material
              changes will be communicated via email or through the platform at
              least 30 days before they take effect. Continued use of the
              platform after the effective date constitutes acceptance of the
              updated policy.
            </p>
          </section>

          {/* Contact */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              13. Contact
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

      <FooterSection />
    </div>
  );
}
