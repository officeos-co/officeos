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
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { PlusIcon, SearchIcon, FilterIcon, MoreHorizontalIcon, Trash2Icon } from "lucide-react";
import { useAgents, useDeleteAgent } from "@/hooks/useAgents";

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
  const { agents, refetch } = useAgents();
  const { deleteAgent } = useDeleteAgent();
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
      <PageHeader group="Managed Agents" page="Agents" />
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
              <th className="px-4 py-3 text-xs font-normal text-center">
                Status
              </th>
              <th className="px-4 py-3 text-xs font-normal">Created</th>
              <th className="px-4 py-3 text-xs font-normal">Last updated</th>
              <th className="px-4 py-3 text-xs font-normal w-10"></th>
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
                <td className="px-4 py-3">
                  <DropdownMenu>
                    <DropdownMenuTrigger
                      render={<Button variant="ghost" size="icon" className="size-8" />}
                      onClick={(e: React.MouseEvent) => e.stopPropagation()}
                    >
                      <MoreHorizontalIcon className="size-4" />
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem
                        className="text-destructive focus:text-destructive"
                        onClick={async () => {
                          await deleteAgent(agent.id);
                          refetch();
                        }}
                      >
                        <Trash2Icon className="size-4 mr-2" />
                        Delete agent
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </td>
              </tr>
            ))}
            {filtered.length === 0 && (
              <tr>
                <td
                  colSpan={7}
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
