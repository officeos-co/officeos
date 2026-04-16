"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuCheckboxItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { PlusIcon, SearchIcon, FilterIcon } from "lucide-react";

const agents = [
  {
    id: "agt_a1b2c3",
    name: "Research Assistant",
    model: "claude-opus-4-6",
    status: "running",
    created: "2 days ago",
    updated: "10 minutes ago",
  },
  {
    id: "agt_b2c3d4",
    name: "Code Reviewer",
    model: "claude-sonnet-4-6",
    status: "running",
    created: "5 days ago",
    updated: "1 hour ago",
  },
  {
    id: "agt_c3d4e5",
    name: "Customer Support Bot",
    model: "gpt-4o",
    status: "running",
    created: "10 days ago",
    updated: "2 hours ago",
  },
  {
    id: "agt_d4e5f6",
    name: "Data Pipeline Monitor",
    model: "claude-haiku-4-5",
    status: "pending",
    created: "1 day ago",
    updated: "1 day ago",
  },
  {
    id: "agt_e5f6a7",
    name: "Content Writer",
    model: "gpt-4o-mini",
    status: "stopped",
    created: "2 weeks ago",
    updated: "7 days ago",
  },
  {
    id: "agt_f6a7b8",
    name: "Security Scanner",
    model: "claude-sonnet-4-6",
    status: "failed",
    created: "3 days ago",
    updated: "30 minutes ago",
  },
];

const ALL_STATUSES = ["running", "pending", "stopped", "failed"] as const;

const statusStyles: Record<
  string,
  { bg: string; text: string; label: string }
> = {
  running: { bg: "bg-emerald-100", text: "text-emerald-700", label: "RUNNING" },
  pending: { bg: "bg-amber-100", text: "text-amber-700", label: "PENDING" },
  stopped: { bg: "bg-zinc-100", text: "text-zinc-500", label: "STOPPED" },
  failed: { bg: "bg-red-100", text: "text-red-700", label: "FAILED" },
};

function StatusBadge({ status }: { status: string }) {
  const style = statusStyles[status] ?? statusStyles.stopped;
  return (
    <span
      className={`inline-flex rounded-full px-2.5 py-1 text-[10px] font-semibold uppercase tracking-widest ${style.bg} ${style.text}`}
    >
      {style.label}
    </span>
  );
}

export default function AgentsPage() {
  const router = useRouter();
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<Set<string>>(new Set());

  const filtered = agents.filter((a) => {
    if (search && !a.name.toLowerCase().includes(search.toLowerCase()))
      return false;
    if (statusFilter.size > 0 && !statusFilter.has(a.status)) return false;
    return true;
  });

  function toggleStatus(status: string) {
    setStatusFilter((prev) => {
      const next = new Set(prev);
      if (next.has(status)) next.delete(status);
      else next.add(status);
      return next;
    });
  }

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page="Agents"
        action={
          <Button size="sm">
            <PlusIcon />
            New agent
          </Button>
        }
      />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        <div className="flex items-center gap-2">
          <div className="relative flex-1 max-w-sm">
            <SearchIcon className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
            <Input
              placeholder="Search agents..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-8"
            />
          </div>
          <DropdownMenu>
            <DropdownMenuTrigger
              render={<Button variant="outline" size="sm" />}
            >
              <FilterIcon className="size-4" />
              Status
              {statusFilter.size > 0 && (
                <span className="ml-1 rounded-full bg-muted px-1.5 text-xs">
                  {statusFilter.size}
                </span>
              )}
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start">
              {ALL_STATUSES.map((s) => (
                <DropdownMenuCheckboxItem
                  key={s}
                  checked={statusFilter.has(s)}
                  onCheckedChange={() => toggleStatus(s)}
                  className="capitalize"
                >
                  {s}
                </DropdownMenuCheckboxItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

        <table className="w-full text-sm">
          <thead>
            <tr className="border-b text-left">
              <th className="px-4 py-3 text-xs font-normal">ID</th>
              <th className="px-4 py-3 text-xs font-normal">Name</th>
              <th className="px-4 py-3 text-xs font-normal">Model</th>
              <th className="px-4 py-3 text-xs font-normal text-center">Status</th>
              <th className="px-4 py-3 text-xs font-normal">Created</th>
              <th className="px-4 py-3 text-xs font-normal">Last updated</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((agent) => (
              <tr
                key={agent.id}
                onClick={() => router.push(`/agents/${agent.id}`)}
                className="border-b last:border-0 hover:bg-muted/50 cursor-pointer transition-colors"
              >
                <td className="px-4 py-3">{agent.id}</td>
                <td className="px-4 py-3">{agent.name}</td>
                <td className="px-4 py-3">{agent.model}</td>
                <td className="px-4 py-3 text-center">
                  <StatusBadge status={agent.status} />
                </td>
                <td className="px-4 py-3">{agent.created}</td>
                <td className="px-4 py-3">{agent.updated}</td>
              </tr>
            ))}
            {filtered.length === 0 && (
              <tr>
                <td
                  colSpan={6}
                  className="px-4 py-8 text-center text-muted-foreground"
                >
                  No agents found.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
