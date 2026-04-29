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
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from "@/components/ui/table";
import { PlusIcon, SearchIcon, FilterIcon, MoreHorizontalIcon, Trash2Icon } from "lucide-react";
import { useAgents, useDeleteAgent } from "@/features/agents";

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
  const { agents, loading, refetch } = useAgents();
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

        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>ID</TableHead>
              <TableHead>Name</TableHead>
              <TableHead>Model</TableHead>
              <TableHead className="text-center">Status</TableHead>
              <TableHead>Created</TableHead>
              <TableHead>Last updated</TableHead>
              <TableHead className="w-10" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && agents.length === 0 && Array.from({ length: 3 }).map((_, i) => (
              <TableRow key={i}>
                <TableCell><Skeleton className="h-4 w-8" /></TableCell>
                <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                <TableCell className="text-center"><Skeleton className="h-6 w-16 rounded-full mx-auto" /></TableCell>
                <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                <TableCell><Skeleton className="size-6 rounded" /></TableCell>
              </TableRow>
            ))}
            {filtered.map((agent) => (
              <TableRow
                key={agent.id}
                onClick={() => router.push(`/agents/${agent.id}`)}
                className="cursor-pointer"
              >
                <TableCell>{agent.id}</TableCell>
                <TableCell>{agent.name}</TableCell>
                <TableCell>{agent.model}</TableCell>
                <TableCell className="text-center">
                  <StatusBadge status={agent.status} />
                </TableCell>
                <TableCell>{agent.created}</TableCell>
                <TableCell>{agent.updated}</TableCell>
                <TableCell>
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
                </TableCell>
              </TableRow>
            ))}
            {!loading && filtered.length === 0 && (
              <TableRow>
                <TableCell
                  colSpan={7}
                  className="py-8 text-center text-muted-foreground"
                >
                  No agents found.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
    </>
  );
}
