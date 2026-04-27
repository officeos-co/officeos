import Image from "next/image";
import Link from "next/link";
import { siteConfig } from "@/lib/site";
import { Navbar } from "@/components/sections/navbar";
import { FooterSection } from "@/components/sections/footer-section";

export const metadata = {
  title: "About — OfficeOS",
  description:
    "The story behind OfficeOS — structuring agentic work for organizations.",
};

export default function About() {
  return (
    <div className="relative mx-auto max-w-7xl border-x">
      <div className="absolute top-0 left-6 z-10 block h-full w-px border-border border-l" />
      <div className="absolute top-0 right-6 z-10 block h-full w-px border-border border-r" />
      <Navbar />

      <main className="flex min-h-screen w-full flex-col items-center">
      <div className="mx-auto max-w-4xl w-full px-6 pt-20 pb-28 md:pt-28">
        <h1 className="text-4xl font-bold tracking-tight text-center md:text-5xl lg:text-6xl">
          The Mission
        </h1>
        <p className="mt-4 text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed text-center">
          Structuring agentic work for organizations — so every team can deploy,
          connect, and control their own AI agents.
        </p>

        <div className="mt-20 space-y-8 text-muted-foreground leading-relaxed text-lg">
          <p>
            AI agents today are unstructured. You spin up a VM, SSH into it,
            install CLI tools, configure API keys manually — and repeat for
            every single agent. Agents don&apos;t know each other. Tools
            aren&apos;t shared. Credentials aren&apos;t centralized. Nothing is
            organized.
          </p>
          <p>
            <strong className="text-primary">
              OfficeOS exists to fix that.
            </strong>{" "}
            We&apos;re building the intelligence layer for autonomous AI agents
            — a Kubernetes-native platform where agents are deployed in seconds,
            share a central knowledge graph, and execute skills that run real
            business logic on your infrastructure.
          </p>
          <p>
            Think of it as{" "}
            <strong className="text-primary">
              the operating system for agentic work
            </strong>
            . Each team gets their own agents with the right skills,
            permissions, and access to your organization&apos;s knowledge.
            Credentials are managed once, used by all agents. Skills are written
            in TypeScript, deployed instantly, and executed in sandboxed V8
            isolates — not containers, not VMs.
          </p>
          <p>
            The architecture is fundamentally different from managed agent
            platforms that run prompts in the cloud with only MCP server access.
            Our agents run as lightweight Rust binaries in Kubernetes pods —
            each using just megabytes of resources — with full system control
            and deep integration into any software that exposes an API.
          </p>
          <p>
            We believe the next era of enterprise software isn&apos;t about
            building more dashboards. It&apos;s about deploying intelligent
            agents that do the work — and giving organizations the tools to
            manage them at scale.
          </p>
        </div>

        {/* Founder */}
        <div className="mt-24">
          <h2 className="text-2xl font-bold tracking-tight text-center mb-12">
            Built by
          </h2>
          <div className="flex flex-col items-center gap-6">
            <div className="relative h-28 w-28 overflow-hidden rounded-full border border-border">
              <Image
                src="/HarroProfile.jpg"
                alt="Harro Krog"
                fill
                className="object-cover"
              />
            </div>
            <div className="text-center">
              <p className="text-lg font-medium">Harro Krog</p>
              <p className="text-sm text-muted-foreground">
                Founder &amp; Engineer
              </p>
              <p className="mt-3 max-w-md text-sm text-muted-foreground leading-relaxed">
                Based in Hamburg. Building OfficeOS as the obvious next step
                after seeing how unstructured agentic work is today — agents
                that don&apos;t know each other, tools that aren&apos;t shared,
                credentials scattered everywhere.
              </p>
            </div>
          </div>
        </div>

        {/* Hackathon */}
        <div className="mt-24">
          <div className="relative w-full overflow-hidden rounded-xl border border-border">
            <Image
              src="/GroupPhoto.jpeg"
              alt="Disability Tech Hackathon at Microsoft Denmark"
              width={1280}
              height={853}
              className="w-full h-auto"
            />
          </div>
          <p className="text-sm text-muted-foreground text-center mt-4">
            Disability Tech Hackathon 2026 — First place at Microsoft Denmark
          </p>
          <p className="text-sm text-muted-foreground text-center mt-1">
            Won by our founder Harro Krog, earning a spot in Microsoft for
            Startups Founders Hub.
          </p>
        </div>

        {/* CTA */}
        <div className="mt-24 text-center">
          <h2 className="text-2xl font-bold tracking-tight mb-4">
            Want to learn more?
          </h2>
          <div className="flex flex-row items-center justify-center gap-3">
            <Link
              href={siteConfig.dashboardUrl}
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
      </div>
      </main>

      <FooterSection />
    </div>
  );
}
