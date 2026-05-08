"use client";

import { use } from "react";
import Link from "next/link";
import { PageContainer } from "@/components/page-container";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";
import { AgentBrowserTab } from "@/features/agents/components/agent-browser-tab";
import { useBrowserResource } from "@/features/agents";

export default function BrowserResourceDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const { browserResource, loading } = useBrowserResource(id);

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page={browserResource?.displayName ?? "Browser"}
        subtitle="Browser resource details."
        width="wide"
        action={
          <Button variant="outline" size="sm" nativeButton={false} render={<Link href="/browser" />}>
            All browsers
          </Button>
        }
      />
      <PageContainer width="wide" className="flex flex-1 flex-col pb-4">
        {browserResource?.currentAgentId ? (
          <AgentBrowserTab agentId={browserResource.currentAgentId} />
        ) : (
          <div className="mt-4 rounded-xl border border-border p-8 text-sm text-muted-foreground">
            {loading
              ? "Loading browser resource..."
              : "Attach this browser to an agent from the agents page to open the live browser details."}
          </div>
        )}
      </PageContainer>
    </>
  );
}
