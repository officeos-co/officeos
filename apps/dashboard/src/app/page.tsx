"use client";

import { useRouter } from "next/navigation";
import { useAgents, type Agent } from "@/hooks/useAgents";
import { useSkills } from "@/hooks/useSkills";
import { useProviders } from "@/hooks/useProviders";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { TopBar } from "@/components/shared/TopBar";
import { EmptyState } from "@/components/shared/EmptyState";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { formatDate } from "@/utils/format";

function statusGroup(agents: Agent[]) {
  let running = 0;
  let pending = 0;
  let errors = 0;
  for (const a of agents) {
    if (a.status === "running" || a.status === "ready" || a.status === "online") running++;
    else if (a.status === "pending" || a.status === "building") pending++;
    else if (a.status === "failed" || a.status === "not_found") errors++;
  }
  return { running, pending, errors };
}

function StatItem({ value, label }: { value: string | number; label: string }) {
  return (
    <div>
      <div className="text-2xl font-semibold tracking-tight text-foreground">{value}</div>
      <div className="text-[12px] text-muted-foreground">{label}</div>
    </div>
  );
}

export default function DashboardPage() {
  const router = useRouter();
  const { agents, loading: agentsLoading } = useAgents();
  const { skills, loading: skillsLoading } = useSkills();
  const { providers } = useProviders();

  const { running, pending, errors } = statusGroup(agents);
  const installedSkills = skills.filter((s) => s.installed).length;
  const configuredProviders = providers.filter((p) => p.configured).length;

  const isLoading = agentsLoading && agents.length === 0;

  return (
    <div>
      <TopBar title="Overview" />

      <div className="px-6 py-6">
        {/* Stats row — flat, no cards */}
        <div className="flex flex-wrap gap-8 pb-6 border-b border-border">
          {isLoading ? (
            Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="space-y-1.5">
                <Skeleton className="h-7 w-10" />
                <Skeleton className="h-3 w-16" />
              </div>
            ))
          ) : (
            <>
              <StatItem value={agents.length} label="Total agents" />
              <StatItem value={running} label="Running" />
              <StatItem value={installedSkills} label="Tools installed" />
              <StatItem value={configuredProviders} label="Providers" />
            </>
          )}
        </div>

        {/* Recent agents table */}
        <div className="mt-6">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-[13px] font-medium text-muted-foreground">Recent agents</h2>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => router.push("/agents")}
              className="text-[12px] text-muted-foreground h-7"
            >
              View all
            </Button>
          </div>

          {isLoading ? (
            <div className="space-y-3">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="flex items-center gap-4 py-2">
                  <Skeleton className="h-4 w-28" />
                  <Skeleton className="h-4 w-20" />
                  <Skeleton className="h-4 w-16" />
                </div>
              ))}
            </div>
          ) : agents.length === 0 ? (
            <EmptyState
              title="No agents yet"
              description="Create your first agent to get started."
              action={
                <Button size="sm" onClick={() => router.push("/agents")}>
                  Create agent
                </Button>
              }
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow className="hover:bg-transparent">
                  <TableHead className="text-[12px] font-normal text-muted-foreground">Name</TableHead>
                  <TableHead className="text-[12px] font-normal text-muted-foreground">Provider</TableHead>
                  <TableHead className="text-[12px] font-normal text-muted-foreground">Model</TableHead>
                  <TableHead className="text-[12px] font-normal text-muted-foreground">Status</TableHead>
                  <TableHead className="text-[12px] font-normal text-muted-foreground text-right">Created</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {agents.slice(0, 5).map((agent) => (
                  <TableRow
                    key={agent.id}
                    onClick={() => router.push(`/agents/${agent.id}`)}
                    className="cursor-pointer"
                  >
                    <TableCell className="text-[13px] font-medium">{agent.name}</TableCell>
                    <TableCell className="text-[13px] text-muted-foreground">{agent.provider}</TableCell>
                    <TableCell className="text-[13px] text-muted-foreground">{agent.model ?? "—"}</TableCell>
                    <TableCell>
                      <StatusBadge status={agent.status} />
                    </TableCell>
                    <TableCell className="text-[13px] text-muted-foreground text-right">
                      {formatDate(agent.createdAt)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </div>
      </div>
    </div>
  );
}
