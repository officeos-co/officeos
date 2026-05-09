"use client";

import Link from "next/link";
import {
  AlertTriangleIcon,
  BotIcon,
  Clock3Icon,
  PlayCircleIcon,
} from "lucide-react";
import { OverviewCard, OverviewCardGrid } from "@/components/overview-card";
import { PageContainer } from "@/components/page-container";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/ui/status-badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useAgents } from "@/features/agents";

export default function Home() {
  const { agents, loading } = useAgents();
  const latestAgents = [...agents]
    .sort((a, b) => b.createdAt - a.createdAt)
    .slice(0, 5);

  const counts = {
    all: agents.length,
    running: agents.filter((agent) => agent.status === "running").length,
    pending: agents.filter((agent) => agent.status === "pending").length,
    failed: agents.filter((agent) => agent.status === "failed").length,
  };

  return (
    <>
      <PageHeader
        page="Overview"
        subtitle="Monitor your agent workspace."
        width="wide"
        action={
          <Button size="sm" nativeButton={false} render={<Link href="/agents" />}>
            <BotIcon className="size-3.5" />
            Agents
          </Button>
        }
      />
      <PageContainer width="wide" className="flex flex-1 flex-col gap-4 pb-4">
        <OverviewCardGrid>
          <OverviewCard
            label="Total agents"
            value={counts.all}
            icon={BotIcon}
            loading={loading && agents.length === 0}
          />
          <OverviewCard
            label="Running"
            value={counts.running}
            icon={PlayCircleIcon}
            loading={loading && agents.length === 0}
          />
          <OverviewCard
            label="Pending"
            value={counts.pending}
            icon={Clock3Icon}
            loading={loading && agents.length === 0}
          />
          <OverviewCard
            label="Failed"
            value={counts.failed}
            icon={AlertTriangleIcon}
            loading={loading && agents.length === 0}
            tone={counts.failed > 0 ? "destructive" : "default"}
          />
        </OverviewCardGrid>

        <section className="rounded-lg border border-border bg-card">
          <div className="flex items-center justify-between gap-3 border-b border-border px-4 py-3">
            <div>
              <h2 className="text-sm font-medium">Latest agents</h2>
              <p className="text-xs text-muted-foreground">
                Recently created agents in this workspace.
              </p>
            </div>
            <Button
              size="sm"
              variant="outline"
              nativeButton={false}
              render={<Link href="/agents" />}
            >
              View all
            </Button>
          </div>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Model</TableHead>
                <TableHead className="text-center">Status</TableHead>
                <TableHead>Created</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading &&
                agents.length === 0 &&
                Array.from({ length: 3 }).map((_, index) => (
                  <TableRow key={index}>
                    <TableCell>
                      <Skeleton className="h-4 w-32" />
                    </TableCell>
                    <TableCell>
                      <Skeleton className="h-4 w-40" />
                    </TableCell>
                    <TableCell className="text-center">
                      <Skeleton className="mx-auto h-6 w-16 rounded-full" />
                    </TableCell>
                    <TableCell>
                      <Skeleton className="h-4 w-24" />
                    </TableCell>
                  </TableRow>
                ))}
              {latestAgents.map((agent) => (
                <TableRow key={agent.id}>
                  <TableCell className="font-medium">
                    <Link
                      href={`/agents/${agent.id}`}
                      className="hover:text-primary hover:underline"
                    >
                      {agent.name}
                    </Link>
                  </TableCell>
                  <TableCell className="text-muted-foreground">
                    {agent.model}
                  </TableCell>
                  <TableCell className="text-center">
                    <StatusBadge status={agent.status} />
                  </TableCell>
                  <TableCell className="text-muted-foreground">
                    {agent.created}
                  </TableCell>
                </TableRow>
              ))}
              {!loading && latestAgents.length === 0 && (
                <TableRow>
                  <TableCell colSpan={4} className="p-0">
                    <EmptyState message="No agents found." />
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </section>
      </PageContainer>
    </>
  );
}
