import Link from "next/link";
import { Navbar } from "@/components/sections/navbar";
import { Cpu, Zap, Shield, Globe, Terminal, RefreshCw } from "lucide-react";

export const metadata = {
  title: "Platform Overview — OfficeOS",
  description:
    "Kubernetes-native platform for autonomous AI agents. Lightweight Rust binaries, single-env deployment, real-time status, and multi-provider LLM support.",
};

export default function PlatformPage() {
  return (
    <div className="min-h-screen bg-background text-primary font-sans">
      <Navbar />

      <main className="mx-auto max-w-4xl px-6 pt-20 pb-28 md:pt-28">
        <h1 className="text-4xl font-bold tracking-tight text-center md:text-5xl">
          The Platform
        </h1>
        <p className="mt-4 text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed text-center">
          A Kubernetes-native runtime where AI agents deploy in seconds, run as
          lightweight Rust binaries, and operate with full system control — not
          just prompt execution in the cloud.
        </p>

        <div className="mt-16 space-y-12 text-muted-foreground leading-relaxed">
          {/* Rust Runtime */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Cpu className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Rust Agent Runtime
              </h2>
            </div>
            <p>
              Every agent runs as a compiled Rust binary called{" "}
              <strong className="text-primary">zeroclaw-core</strong>. This
              isn&apos;t a Python script inside a Docker container or a VM you
              SSH into. It&apos;s a purpose-built runtime with a turn loop, tool
              execution engine, memory management, and channel system — all
              running natively in a Kubernetes pod.
            </p>
            <p className="mt-4">
              Each agent requests just{" "}
              <strong className="text-primary">64Mi of memory</strong> with a
              512Mi limit. That&apos;s roughly 100x more efficient than VM-based
              alternatives like OpenClaw, where each agent needs its own virtual
              machine with a full operating system. You can run hundreds of
              agents on hardware that would support a handful of VMs.
            </p>
          </section>

          {/* Single Env Deployment */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Terminal className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Single Environment Variable Deployment
              </h2>
            </div>
            <p>
              Agent pods receive exactly one environment variable:{" "}
              <strong className="text-primary">ZEROCLAW_AGENT_ID</strong>.
              Everything else — the LLM provider, model selection, vault
              location, available skills, personality files — is derived from
              that single ID by calling the backend at boot time.
            </p>
            <p className="mt-4">
              This design eliminates configuration drift, simplifies deployment
              manifests, and means agent pods are stateless and reproducible. If
              a pod crashes, Kubernetes restarts it with the same ID and the
              agent hydrates its full state from the backend and CouchDB vault
              automatically.
            </p>
          </section>

          {/* Agent Lifecycle */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Zap className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Agent Lifecycle
              </h2>
            </div>
            <p>
              Creating an agent follows a deterministic four-step pipeline:{" "}
              <strong className="text-primary">
                create → provision CouchDB vault → deploy K8s pod → running
              </strong>
              . The backend stores the agent record in Postgres, provisions a
              dedicated CouchDB database with personality files, generates a
              Kubernetes deployment manifest, and applies it to the cluster.
            </p>
            <p className="mt-4">
              From button click to a running agent with a WebSocket chat gateway
              takes seconds, not minutes. The agent pod boots, calls the backend
              to hydrate its workspace, discovers available skills via GraphQL
              introspection, and is ready to receive instructions over its
              real-time WebSocket connection.
            </p>
          </section>

          {/* LLM Proxy */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Shield className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                LLM Proxy Architecture
              </h2>
            </div>
            <p>
              Agents never see API keys. Every LLM call routes through the
              backend&apos;s proxy layer, which injects the correct credentials
              per-request based on the agent&apos;s configured provider. This
              means API keys are stored exactly once in the backend — not in
              environment variables, not in pod specs, not in config files on
              disk.
            </p>
            <p className="mt-4">
              The proxy supports{" "}
              <strong className="text-primary">
                OpenAI, Anthropic, Groq, DeepSeek, xAI, OpenRouter, and Ollama
              </strong>
              . Switching an agent&apos;s provider or model is an API call — the
              change takes effect on the very next LLM request without requiring
              a pod restart. You can run different agents on different providers
              simultaneously, optimizing for cost, latency, or capability per
              use case.
            </p>
          </section>

          {/* Real-time Status */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <RefreshCw className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Real-Time Status
              </h2>
            </div>
            <p>
              The dashboard doesn&apos;t cache pod status in a database. When
              you open the agent list, the backend calls the Kubernetes API
              inline to refresh every agent&apos;s actual pod state — running,
              pending, crashed, or terminated. The frontend polls every 10
              seconds, so you always see the truth, not a stale snapshot.
            </p>
            <p className="mt-4">
              This extends to resource usage, restart counts, and health checks.
              If an agent pod is evicted or rescheduled by Kubernetes,
              you&apos;ll see it immediately in Mission Control without waiting
              for a background sync job.
            </p>
          </section>

          {/* WebSocket */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Globe className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                WebSocket Chat Gateway
              </h2>
            </div>
            <p>
              Every agent exposes a WebSocket endpoint for real-time,
              bidirectional communication. Messages stream token by token as the
              LLM generates them. Tool calls, skill executions, and status
              updates are pushed to connected clients as they happen — no
              polling, no waiting for complete responses.
            </p>
            <p className="mt-4">
              The gateway supports multiple simultaneous connections per agent,
              making it possible for team members to observe an agent&apos;s
              work in real time or for external systems to consume agent output
              programmatically.
            </p>
          </section>
        </div>

        {/* CTA */}
        <div className="mt-24 text-center">
          <h2 className="text-2xl font-bold tracking-tight mb-4">
            See the platform in action
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
        Made in Hamburg — © 2026 OfficeOS GmbH
      </footer>
    </div>
  );
}
