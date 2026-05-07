"use client";

import Link from "next/link";
import { DatabaseZapIcon, SearchIcon, ShieldCheckIcon } from "lucide-react";
import { PageContainer } from "@/components/page-container";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";

const features = [
  {
    icon: SearchIcon,
    title: "Indexed search",
    copy: "Agents query a local searchable replica instead of crawling APIs in real time.",
  },
  {
    icon: DatabaseZapIcon,
    title: "GitHub first",
    copy: "V1 supports repositories, issues, pull requests, and commits.",
  },
  {
    icon: ShieldCheckIcon,
    title: "Shared auth",
    copy: "Atlas uses the same dashboard session and GitHub OAuth connection.",
  },
];

export default function AtlasPage() {
  return (
    <>
      <PageHeader
        page="Atlas"
        subtitle="Searchable company context for OfficeOS agents."
        width="wide"
        action={
          <Button size="sm" render={<Link href="/atlas/connectors" />}>
            <DatabaseZapIcon className="size-4" />
            Connect GitHub
          </Button>
        }
      />
      <PageContainer width="wide" className="pb-10">
        <section className="grid gap-8 py-8 lg:grid-cols-[1.1fr_0.9fr]">
          <div className="flex flex-col justify-center">
            <h2 className="max-w-3xl text-4xl font-semibold tracking-tight">
              Give agents a searchable replica of your operational data.
            </h2>
            <p className="mt-5 max-w-2xl text-base leading-7 text-muted-foreground">
              Atlas indexes selected connector entities into OfficeOS so agents can
              search context quickly, then fall back to direct API calls when they
              need fresh records.
            </p>
            <div className="mt-6 flex gap-3">
              <Button render={<Link href="/atlas/connectors" />}>
                Open connectors
              </Button>
              <Button variant="outline" render={<Link href="/atlas/history" />}>
                View history
              </Button>
            </div>
          </div>

          <div className="rounded-lg border border-border bg-card p-5">
            <div className="mb-4 flex items-center gap-3">
              <div className="flex size-10 items-center justify-center rounded-md bg-primary/10 text-primary">
                <DatabaseZapIcon className="size-5" />
              </div>
              <div>
                <h3 className="font-medium">Context Store</h3>
                <p className="text-sm text-muted-foreground">
                  Indexed GitHub context for agent tool calls
                </p>
              </div>
            </div>
            <div className="space-y-3 text-sm">
              {[
                ["repositories", "context_store_search", "Ready"],
                ["pull_requests", "list", "Direct"],
                ["commits", "list", "Direct"],
              ].map(([entity, action, status]) => (
                <div
                  key={`${entity}-${action}`}
                  className="flex items-center justify-between rounded-md border border-border px-3 py-2"
                >
                  <span className="font-mono text-xs">{entity}</span>
                  <span className="text-muted-foreground">{action}</span>
                  <span className="rounded-full bg-muted px-2 py-0.5 text-xs">
                    {status}
                  </span>
                </div>
              ))}
            </div>
          </div>
        </section>

        <section className="grid gap-4 md:grid-cols-3">
          {features.map(({ icon: Icon, title, copy }) => (
            <div key={title} className="rounded-lg border border-border p-4">
              <Icon className="mb-3 size-5 text-primary" />
              <h3 className="font-medium">{title}</h3>
              <p className="mt-2 text-sm leading-6 text-muted-foreground">
                {copy}
              </p>
            </div>
          ))}
        </section>
      </PageContainer>
    </>
  );
}
