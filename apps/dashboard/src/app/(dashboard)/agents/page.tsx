"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { PageHeader } from "@/components/page-header";
import { PageContainer } from "@/components/page-container";
import { Button } from "@/components/ui/button";
import { HelpTooltip, WithTooltip } from "@/components/ui/help-tooltip";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuCheckboxItem,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
  TableSelectionHead,
  TableSelectionCell,
  TableSelectionToolbar,
} from "@/components/ui/table";
import { SearchInput } from "@/components/ui/search-input";
import { StatusBadge } from "@/components/ui/status-badge";
import { EmptyState } from "@/components/ui/empty-state";
import { FilterIcon, MoreHorizontalIcon, Trash2Icon } from "lucide-react";
import { useAgents, useDeleteAgent } from "@/features/agents";

const ALL_STATUSES = ["running", "pending", "stopped", "failed"] as const;

export default function AgentsPage() {
  const router = useRouter();
  const { agents, loading, refetch } = useAgents();
  const { deleteAgent } = useDeleteAgent();
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<Set<string>>(new Set());
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());

  const filtered = agents.filter((a) => {
    if (search && !a.name.toLowerCase().includes(search.toLowerCase()))
      return false;
    if (statusFilter.size > 0 && !statusFilter.has(a.status)) return false;
    return true;
  });
  const filteredIds = useMemo(
    () => filtered.map((agent) => agent.id),
    [filtered],
  );
  const selectedVisibleCount = filteredIds.filter((id) =>
    selectedIds.has(id),
  ).length;
  const allVisibleSelected =
    filteredIds.length > 0 && selectedVisibleCount === filteredIds.length;
  const someVisibleSelected = selectedVisibleCount > 0 && !allVisibleSelected;

  function toggleStatus(status: string) {
    setStatusFilter((prev) => {
      const next = new Set(prev);
      if (next.has(status)) next.delete(status);
      else next.add(status);
      return next;
    });
  }

  function toggleAgent(agentId: string, checked: boolean) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (checked) next.add(agentId);
      else next.delete(agentId);
      return next;
    });
  }

  function toggleVisibleAgents(checked: boolean) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      for (const id of filteredIds) {
        if (checked) next.add(id);
        else next.delete(id);
      }
      return next;
    });
  }

  async function deleteSelectedAgents() {
    const ids = Array.from(selectedIds);
    await Promise.all(ids.map((id) => deleteAgent(id)));
    setSelectedIds(new Set());
    refetch();
  }

  return (
    <>
      <PageHeader
        page="Agents"
        subtitle="Create and manage autonomous agents."
        width="wide"
      />
      <PageContainer width="wide" className="flex flex-1 flex-col gap-4 pb-4">
        <div className="flex min-h-9 items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <SearchInput
              placeholder="Search agents..."
              value={search}
              onChange={setSearch}
            />
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
          <TableSelectionToolbar selectedCount={selectedIds.size}>
            <Button
              variant="destructive"
              size="sm"
              onClick={deleteSelectedAgents}
            >
              <Trash2Icon className="size-4" />
              Delete
            </Button>
          </TableSelectionToolbar>
        </div>

        <Table>
          <TableHeader>
            <TableRow>
              <TableSelectionHead
                checked={allVisibleSelected}
                indeterminate={someVisibleSelected}
                onCheckedChange={toggleVisibleAgents}
              />
              <TableHead>ID</TableHead>
              <TableHead>Name</TableHead>
              <TableHead>
                <span className="inline-flex items-center gap-1.5">
                  Model
                  <HelpTooltip>
                    Auto means a transparent backend routing policy. Concrete
                    model IDs are sent directly to the configured provider.
                  </HelpTooltip>
                </span>
              </TableHead>
              <TableHead className="text-center">
                <span className="inline-flex items-center justify-center gap-1.5">
                  Status
                  <HelpTooltip>
                    Running agents can receive messages. Pending agents are
                    still starting; failed agents need operator attention.
                  </HelpTooltip>
                </span>
              </TableHead>
              <TableHead>Created</TableHead>
              <TableHead>Last updated</TableHead>
              <TableHead className="w-10" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading &&
              agents.length === 0 &&
              Array.from({ length: 3 }).map((_, i) => (
                <TableRow key={i}>
                  <TableCell className="w-10 px-3">
                    <Skeleton className="size-4 rounded" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-8" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-28" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-32" />
                  </TableCell>
                  <TableCell className="text-center">
                    <Skeleton className="h-6 w-16 rounded-full mx-auto" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-24" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-24" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="size-6 rounded" />
                  </TableCell>
                </TableRow>
              ))}
            {filtered.map((agent) => (
              <TableRow
                key={agent.id}
                data-state={selectedIds.has(agent.id) ? "selected" : undefined}
                onClick={() => router.push(`/agents/${agent.id}`)}
                className="cursor-pointer"
              >
                <TableSelectionCell
                  checked={selectedIds.has(agent.id)}
                  aria-label={`Select ${agent.name}`}
                  onCheckedChange={(checked) => toggleAgent(agent.id, checked)}
                />
                <TableCell>{agent.id}</TableCell>
                <TableCell>{agent.name}</TableCell>
                <TableCell>
                  <WithTooltip tooltip={agent.model === "auto" ? "Auto is only exposed when Anthropic smart routing is configured." : "Concrete model. Requests go directly to this model."}>
                    <span>{agent.model}</span>
                  </WithTooltip>
                </TableCell>
                <TableCell className="text-center">
                  <StatusBadge status={agent.status} />
                </TableCell>
                <TableCell>{agent.created}</TableCell>
                <TableCell>{agent.updated}</TableCell>
                <TableCell>
                  <DropdownMenu>
                    <DropdownMenuTrigger
                      render={
                        <Button
                          variant="ghost"
                          size="icon"
                          className="size-8"
                        />
                      }
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
                <TableCell colSpan={8} className="p-0">
                  <EmptyState message="No agents found." />
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </PageContainer>
    </>
  );
}
