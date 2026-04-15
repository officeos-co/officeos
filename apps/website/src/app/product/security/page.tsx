import Link from "next/link";
import { Navbar } from "@/components/sections/navbar";
import {
  ShieldCheck,
  KeyRound,
  Eye,
  Container,
  Lock,
  FileCheck,
} from "lucide-react";

export const metadata = {
  title: "Enterprise Security — OfficeOS",
  description:
    "Self-hosted, single-tenant architecture. Credentials never leave the backend. Sandboxed execution, pod isolation, encrypted storage, audit trails, and GDPR compliance.",
};

export default function SecurityPage() {
  return (
    <div className="min-h-screen bg-background text-primary font-sans">
      <Navbar />

      <main className="mx-auto max-w-4xl px-6 pt-20 pb-28 md:pt-28">
        <h1 className="text-4xl font-bold tracking-tight text-center md:text-5xl">
          Enterprise Security
        </h1>
        <p className="mt-4 text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed text-center">
          Self-hosted, single-tenant architecture where your data never leaves
          your infrastructure. Every credential, every execution, every
          interaction — under your control.
        </p>

        <div className="mt-16 space-y-12 text-muted-foreground leading-relaxed">
          {/* Self-Hosted */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <ShieldCheck className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Self-Hosted, Single-Tenant
              </h2>
            </div>
            <p>
              OfficeOS runs entirely on your infrastructure. There is no
              multi-tenant cloud service, no shared compute, no data leaving
              your network. The platform deploys to your Kubernetes cluster —
              whether that&apos;s on-premise bare metal, a managed cloud
              provider, or a hybrid setup.
            </p>
            <p className="mt-4">
              Single-tenant means{" "}
              <strong className="text-primary">
                your data is physically isolated
              </strong>
              . Not logically isolated within a shared database, not separated
              by row-level security in a multi-tenant schema. Your Postgres
              instance, your CouchDB databases, your Kubernetes namespace — all
              exclusively yours. There is no scenario where another
              customer&apos;s query touches your data.
            </p>
          </section>

          {/* Credential Security */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <KeyRound className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Zero-Trust Credential Architecture
              </h2>
            </div>
            <p>
              <strong className="text-primary">
                Agent pods have zero API keys.
              </strong>{" "}
              This is the foundational security principle of the platform.
              Agents never receive, store, or transmit credentials. All LLM
              calls route through the backend&apos;s proxy, which injects
              credentials per-request. All skill executions pass through the
              GraphQL gateway, which decrypts and injects API keys at the moment
              of execution.
            </p>
            <p className="mt-4">
              Credentials are encrypted using{" "}
              <strong className="text-primary">
                ASP.NET Core DataProtection
              </strong>{" "}
              and stored in Postgres. Decryption happens exclusively in the
              backend process, in memory, for the duration of a single request.
              If an agent pod is compromised, the attacker gains access to
              nothing — no API keys, no tokens, no service account credentials.
            </p>
          </section>

          {/* Audit Trail */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Eye className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Full Audit Trail
              </h2>
            </div>
            <p>
              Every skill execution, every tool call, and every LLM interaction
              is logged. The LLM proxy architecture means all AI calls pass
              through a single auditable chokepoint. You can see exactly which
              agent called which model, with what input, at what time, and what
              the response was.
            </p>
            <p className="mt-4">
              The same applies to skill executions. When an agent creates a
              GitHub issue, sends a Slack message, or queries a database, the
              execution is logged with the full request and response. This gives
              security teams complete visibility into what agents are doing —
              not just that they ran, but exactly what actions they took and
              what data they accessed.
            </p>
          </section>

          {/* Pod Isolation */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Container className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Kubernetes Pod Isolation
              </h2>
            </div>
            <p>
              Each agent runs in its own Kubernetes pod with{" "}
              <strong className="text-primary">defined resource limits</strong>{" "}
              — 64Mi memory request, 512Mi limit. Pods are isolated by default
              through Kubernetes namespace and network policies. A misbehaving
              agent cannot consume cluster resources or access other
              agents&apos; data.
            </p>
            <p className="mt-4">
              Skills execute in a separate, sandboxed Node.js runtime service —
              not inside agent pods. This double isolation means that even if an
              agent could somehow escape its pod, skill execution happens in a
              completely different process with its own resource constraints and
              network boundaries. Browser sessions are similarly isolated:
              per-agent cookie storage ensures agents cannot access each
              other&apos;s authenticated sessions.
            </p>
          </section>

          {/* Access Control */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Lock className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Permission Model
              </h2>
            </div>
            <p>
              Agents only access the skills and data they&apos;re authorized
              for. Role-based access controls in the backend determine which
              agents can execute which skills, access which knowledge documents,
              and communicate through which channels.
            </p>
            <p className="mt-4">
              This extends to the runner system. Self-hosted runners
              authenticate using{" "}
              <strong className="text-primary">
                RFC 8628 device authorization flow
              </strong>{" "}
              with 15-minute code expiration and rate limiting. Runner
              registration requires explicit browser-based approval — there is
              no way to silently register a runner. Once connected, runners only
              execute skills they&apos;re authorized to run, enforced by the
              backend.
            </p>
          </section>

          {/* Compliance */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <FileCheck className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                GDPR Compliance
              </h2>
            </div>
            <p>
              OfficeOS includes a built-in{" "}
              <strong className="text-primary">memory_export tool</strong> for
              data portability — a core GDPR requirement. Users can export all
              data associated with their agents, including conversation history,
              knowledge documents, personality files, and execution logs.
            </p>
            <p className="mt-4">
              The self-hosted architecture inherently simplifies compliance.
              Your data resides in your infrastructure, under your data
              processing agreements, subject to your retention policies.
              There&apos;s no third-party data processor to audit, no
              cross-border data transfer to justify, no sub-processor chain to
              manage. You control the full data lifecycle from creation to
              deletion.
            </p>
          </section>
        </div>

        {/* CTA */}
        <div className="mt-24 text-center">
          <h2 className="text-2xl font-bold tracking-tight mb-4">
            Deploy on your infrastructure
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
        Made in Hamburg — © 2026 OfficeOS
      </footer>
    </div>
  );
}
