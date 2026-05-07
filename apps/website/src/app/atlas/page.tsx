import { Navbar } from "@/components/sections/navbar";
import { FooterSection } from "@/components/sections/footer-section";
import { Button } from "@/components/ui/button";
import { DatabaseZap, Github, Search, ShieldCheck } from "lucide-react";
import Link from "next/link";

const atlasFeatures = [
  {
    icon: Search,
    title: "Fast indexed search",
    copy: "Agents find relevant records without paging through APIs.",
  },
  {
    icon: Github,
    title: "GitHub first",
    copy: "Repositories, issues, pull requests, and commits ship in V1.",
  },
  {
    icon: ShieldCheck,
    title: "Same auth model",
    copy: "Atlas uses the same OfficeOS dashboard and connector credentials.",
  },
];

export default function AtlasMarketingPage() {
  return (
    <main className="min-h-screen bg-background text-foreground">
      <Navbar />
      <section className="mx-auto grid min-h-[calc(100vh-88px)] max-w-7xl gap-10 px-6 pb-14 pt-20 lg:grid-cols-[1.05fr_0.95fr] lg:items-center">
        <div>
          <div className="mb-5 inline-flex items-center gap-2 rounded-full border border-border px-3 py-1 text-sm text-muted-foreground">
            <DatabaseZap className="size-4" />
            OfficeOS Atlas
          </div>
          <h1 className="max-w-3xl text-5xl font-semibold tracking-tight md:text-6xl">
            Searchable context for self-hosted agents.
          </h1>
          <p className="mt-6 max-w-2xl text-lg leading-8 text-muted-foreground">
            Atlas turns connected tools into an indexed Context Store, so agents
            can search company data quickly and call live APIs when freshness
            matters.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <Button asChild>
              <Link href="/pricing">Start with OfficeOS</Link>
            </Button>
            <Button variant="outline" asChild>
              <a href="https://docs.officeos.co">Read docs</a>
            </Button>
          </div>
        </div>

        <div className="rounded-lg border border-border bg-card p-4 shadow-sm">
          <div className="mb-4 flex items-center gap-3 border-b border-border pb-4">
            <div className="flex size-11 items-center justify-center rounded-md bg-foreground text-background">
              <Github className="size-6" />
            </div>
            <div>
              <div className="font-medium">GitHub</div>
              <div className="text-sm text-muted-foreground">
                Context Store ready
              </div>
            </div>
          </div>
          <div className="space-y-3">
            {[
              ["Search", "repositories", "context_store_search", "1186ms"],
              ["Direct", "pull_requests", "list", "6407ms"],
              ["Direct", "commits", "list", "5745ms"],
            ].map(([type, entity, action, duration]) => (
              <div
                key={`${entity}-${action}`}
                className="grid grid-cols-[80px_1fr_1fr_72px] items-center gap-3 rounded-md border border-border px-3 py-3 text-sm"
              >
                <span className="text-muted-foreground">{type}</span>
                <span className="font-mono text-xs">{entity}</span>
                <span className="font-mono text-xs text-muted-foreground">
                  {action}
                </span>
                <span className="text-right text-xs">{duration}</span>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="border-t border-border">
        <div className="mx-auto grid max-w-7xl gap-4 px-6 py-16 md:grid-cols-3">
          {atlasFeatures.map(({ icon: Icon, title, copy }) => (
            <div key={title} className="rounded-lg border border-border p-5">
              <Icon className="mb-4 size-5 text-primary" />
              <h2 className="font-medium">{title}</h2>
              <p className="mt-2 text-sm leading-6 text-muted-foreground">
                {copy}
              </p>
            </div>
          ))}
        </div>
      </section>
      <FooterSection />
    </main>
  );
}
