import Link from "next/link";
import { getSiteConfig } from "@/lib/site";
import { Navbar } from "@/components/sections/navbar";
import { IntegrationsContent } from "./integrations-content";

export const metadata = {
  title: "Deep Integrations — OfficeOS",
  description:
    "Real API integrations that work like native tools. GitHub, Notion, Google, Slack, browser automation, self-hosted runners, and a unified GraphQL skill interface.",
};

export default function IntegrationsPage() {
  const siteConfig = getSiteConfig();
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

        <IntegrationsContent />

        {/* CTA */}
        <div className="mt-24 text-center">
          <h2 className="text-2xl font-bold tracking-tight mb-4">
            Connect your tools
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
      </main>

      <footer className="border-t border-border py-8 text-center text-sm text-muted-foreground">
        Made in Hamburg — © 2026 OfficeOS
      </footer>
    </div>
  );
}
