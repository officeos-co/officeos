import Link from "next/link";
import { Navbar } from "@/components/sections/navbar";
import { Code, Box, Layers, Lock, Upload, Sparkles } from "lucide-react";

export const metadata = {
  title: "Skills & SDK — OfficeOS",
  description:
    "TypeScript SDK for defining agent skills with Zod validation. Sandboxed execution, dynamic GraphQL schema, credential isolation, and hot-loading — no Docker rebuild.",
};

export default function SkillsPage() {
  return (
    <div className="min-h-screen bg-background text-primary font-sans">
      <Navbar />

      <main className="mx-auto max-w-4xl px-6 pt-20 pb-28 md:pt-28">
        <h1 className="text-4xl font-bold tracking-tight text-center md:text-5xl">
          Skills &amp; SDK
        </h1>
        <p className="mt-4 text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed text-center">
          Define agent capabilities in TypeScript with Zod schemas. Skills
          execute in sandboxed runtimes, deploy without Docker rebuilds, and are
          discovered by agents automatically via GraphQL introspection.
        </p>

        <div className="mt-16 space-y-12 text-muted-foreground leading-relaxed">
          {/* SDK */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Code className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                The Skill SDK
              </h2>
            </div>
            <p>
              Skills are defined using{" "}
              <strong className="text-primary">@harro/skill-sdk</strong> — a
              TypeScript package that provides the{" "}
              <strong className="text-primary">defineSkill()</strong> function.
              Each skill is self-contained: it declares a title, emoji,
              description, credential requirements, and a set of actions. Action
              inputs and outputs are validated with Zod schemas, giving you
              compile-time type safety and runtime input validation in a single
              definition.
            </p>
            <p className="mt-4">
              A skill is a plain npm package. You write it in TypeScript, test
              it locally, and publish it. No containers to build, no
              infrastructure to manage, no custom deployment pipeline to
              maintain. The SDK handles manifest generation, schema validation,
              and runtime integration.
            </p>
          </section>

          {/* Sandboxed Execution */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Box className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Sandboxed Execution
              </h2>
            </div>
            <p>
              Skills execute in a dedicated{" "}
              <strong className="text-primary">Node.js skill-runtime</strong>{" "}
              service — not in the agent pod, not in a container per skill, and
              not in a VM. The runtime loads bundled skill packages, exposes an
              HTTP API, and executes actions in isolated contexts.
            </p>
            <p className="mt-4">
              Skills are pre-loaded at runtime boot and ready to execute
              immediately. There&apos;s no container spin-up, no VM
              provisioning, no waiting for a sandbox to initialize. When an
              agent calls a skill, the action executes and returns results in
              the same request cycle.
            </p>
          </section>

          {/* Three-Layer Architecture */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Layers className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Three-Layer Architecture
              </h2>
            </div>
            <p>The skill system is built in three cleanly separated layers:</p>
            <ul className="mt-4 space-y-3 pl-6 list-disc">
              <li>
                <strong className="text-primary">Skill packages</strong> —
                TypeScript modules built with the SDK. Each package exports a
                skill manifest with actions, Zod schemas, and credential
                definitions.
              </li>
              <li>
                <strong className="text-primary">Skill runtime</strong> — A
                Node.js service that loads skill packages, executes actions, and
                exposes an HTTP API. It also serves manifests that describe
                every available skill and its schema.
              </li>
              <li>
                <strong className="text-primary">Backend gateway</strong> — The
                C# backend reads manifests from the runtime and generates a
                dynamic GraphQL schema using HotChocolate&apos;s ITypeModule.
                Zero hardcoded skill knowledge in the backend — everything is
                derived from runtime manifests at startup.
              </li>
            </ul>
            <p className="mt-4">
              This separation means you can add, update, or remove skills
              without touching the backend code. The GraphQL schema regenerates
              automatically from whatever the runtime reports.
            </p>
          </section>

          {/* Credential Isolation */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Lock className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Credential Isolation
              </h2>
            </div>
            <p>
              Skills often need API keys — GitHub tokens, Notion integration
              keys, Google service account credentials. In OfficeOS, these
              credentials are{" "}
              <strong className="text-primary">
                encrypted in Postgres using ASP.NET Core DataProtection
              </strong>{" "}
              and decrypted only at the moment of execution, in the backend
              process, for the duration of a single request.
            </p>
            <p className="mt-4">
              Credentials are never sent to agent pods. They&apos;re never
              stored in environment variables. They&apos;re never written to
              disk outside the database. The agent calls a skill through the
              GraphQL gateway, the backend injects the required credentials,
              forwards the request to the skill runtime, and returns the result.
              The agent never knows the credential existed.
            </p>
          </section>

          {/* Custom Skills */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Upload className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Custom Skill Upload
              </h2>
            </div>
            <p>
              Publishing a custom skill follows a straightforward pipeline:{" "}
              <strong className="text-primary">
                dashboard → validation → storage → hot-load
              </strong>
              . Upload your skill package through the Mission Control dashboard.
              The backend validates the manifest and Zod schemas, stores the
              package in the Postgres-backed skill registry, and hot-loads it
              into the running skill runtime via the <code>/install</code>{" "}
              endpoint.
            </p>
            <p className="mt-4">
              No Docker rebuild. No redeployment. No downtime. Your custom skill
              is available to agents within seconds of upload. The{" "}
              <code>/uninstall</code> endpoint removes skills just as cleanly.
              Skills can be shared across your organization through the skill
              marketplace, making it easy for teams to build and distribute
              capabilities.
            </p>
          </section>

          {/* Auto-Discovery */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Sparkles className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Automatic Skill Discovery
              </h2>
            </div>
            <p>
              Agents don&apos;t need to be told what skills exist. When an agent
              boots, it performs{" "}
              <strong className="text-primary">GraphQL introspection</strong>{" "}
              against the backend gateway and discovers every available skill,
              its actions, input schemas, and descriptions. The agent presents
              these as a CLI-style{" "}
              <strong className="text-primary">skill_exec</strong> tool to the
              LLM.
            </p>
            <p className="mt-4">
              System skills like the browser (powered by Playwright) are always
              available without manual installation or credential configuration.
              When you add a new skill to the runtime, every agent discovers it
              on their next introspection cycle — no agent restarts required.
            </p>
          </section>
        </div>

        {/* CTA */}
        <div className="mt-24 text-center">
          <h2 className="text-2xl font-bold tracking-tight mb-4">
            Build your first skill
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
