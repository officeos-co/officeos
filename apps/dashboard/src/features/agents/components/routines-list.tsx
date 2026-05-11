"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { CalendarClockIcon, GitBranchIcon, PlusIcon, Trash2Icon } from "lucide-react";
import { PageContainer } from "@/shell/page-container";
import { PageHeader } from "@/shell/page-header";
import { Button } from "@/ui/button";
import { EmptyState } from "@/ui/empty-state";
import { SearchInput } from "@/ui/search-input";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  TableSelectionCell,
  TableSelectionHead,
  TableSelectionToolbar,
} from "@/ui/table";
import { useAgents } from "../api/useAgents";
import { useAllRoutines } from "../api/useRoutines";
import {
  routineScheduleSummary,
  routineTriggerSummary,
} from "./routine-display";
import { RoutineCreateDialog } from "./routine-create-dialog";

export function RoutinesList() {
  const router = useRouter();
  const [createOpen, setCreateOpen] = useState(false);
  const { routines, loading, creating, createRoutine, deleteRoutine, refetch } =
    useAllRoutines();
  const { agents } = useAgents();
  const [search, setSearch] = useState("");
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());

  const filtered = useMemo(() => {
    const query = search.toLowerCase();
    return routines.filter((routine) => {
      if (!query) return true;
      return [
        routine.id,
        routine.name,
        routine.agentName,
        routine.prompt,
        routineTriggerSummary(routine),
        routineScheduleSummary(routine),
        routine.enabled ? "enabled" : "disabled",
      ]
        .filter(Boolean)
        .some((value) => value!.toLowerCase().includes(query));
    });
  }, [routines, search]);
  const filteredIds = useMemo(() => filtered.map((routine) => routine.id), [filtered]);
  const selectedVisibleCount = filteredIds.filter((id) =>
    selectedIds.has(id),
  ).length;
  const allVisibleSelected =
    filteredIds.length > 0 && selectedVisibleCount === filteredIds.length;
  const someVisibleSelected = selectedVisibleCount > 0 && !allVisibleSelected;

  function toggleRoutine(routineId: string, checked: boolean) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (checked) next.add(routineId);
      else next.delete(routineId);
      return next;
    });
  }

  function toggleVisibleRoutines(checked: boolean) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      for (const id of filteredIds) {
        if (checked) next.add(id);
        else next.delete(id);
      }
      return next;
    });
  }

  async function deleteSelectedRoutines() {
    const ids = Array.from(selectedIds);
    await Promise.all(ids.map((id) => deleteRoutine(id)));
    setSelectedIds(new Set());
    refetch();
  }

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page="Routines"
        subtitle="Manage scheduled, API, and GitHub-triggered agent routines."
        width="wide"
        action={
          <Button size="sm" onClick={() => setCreateOpen(true)}>
            <PlusIcon className="size-4" />
            Create
          </Button>
        }
      />
      <PageContainer width="wide" className="flex flex-1 flex-col gap-4 pb-4">
        <div className="flex min-h-9 items-center justify-between gap-2">
          <SearchInput
            placeholder="Search routines..."
            value={search}
            onChange={setSearch}
          />
          <TableSelectionToolbar selectedCount={selectedIds.size}>
            <Button
              variant="destructive"
              size="sm"
              onClick={deleteSelectedRoutines}
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
                onCheckedChange={toggleVisibleRoutines}
              />
              <TableHead>ID</TableHead>
              <TableHead>Name</TableHead>
              <TableHead>Agent</TableHead>
              <TableHead>Triggers</TableHead>
              <TableHead>Schedule</TableHead>
              <TableHead>Status</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filtered.map((routine) => (
              <TableRow
                key={routine.id}
                data-state={
                  selectedIds.has(routine.id) ? "selected" : undefined
                }
                onClick={() => router.push(`/routines/${routine.id}`)}
                className="cursor-pointer"
              >
                <TableSelectionCell
                  checked={selectedIds.has(routine.id)}
                  aria-label={`Select ${routine.name}`}
                  onCheckedChange={(checked) =>
                    toggleRoutine(routine.id, checked)
                  }
                />
                <TableCell>{routine.id}</TableCell>
                <TableCell>
                  <span className="inline-flex items-center gap-2 font-medium">
                    <GitBranchIcon className="size-3.5 text-muted-foreground" />
                    {routine.name}
                  </span>
                </TableCell>
                <TableCell>{routine.agentName}</TableCell>
                <TableCell>{routineTriggerSummary(routine)}</TableCell>
                <TableCell>
                  <span className="inline-flex items-center gap-1.5">
                    <CalendarClockIcon className="size-3.5 text-muted-foreground" />
                    {routineScheduleSummary(routine)}
                  </span>
                </TableCell>
                <TableCell>{routine.enabled ? "Enabled" : "Disabled"}</TableCell>
              </TableRow>
            ))}
            {!loading && filtered.length === 0 && (
              <TableRow>
                <TableCell colSpan={7} className="p-0">
                  <EmptyState message="No routines found." />
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </PageContainer>

      <RoutineCreateDialog
        open={createOpen}
        onOpenChange={setCreateOpen}
        agents={agents}
        creating={creating}
        createRoutine={createRoutine}
        onCreated={() => refetch()}
      />
    </>
  );
}
