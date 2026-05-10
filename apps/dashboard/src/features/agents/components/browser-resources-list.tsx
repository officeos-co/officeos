"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { PlusIcon, Trash2Icon } from "lucide-react";
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
import {
  useBrowserResources,
  useCreateBrowserResource,
  useDeleteBrowserResource,
} from "../api/useAgentResources";
import { ResourceCreateDialog } from "./resource-create-dialog";
import { useCanManageWorkspaceFeatures } from "@/features/manage";

export function BrowserResourcesList() {
  const router = useRouter();
  const [createOpen, setCreateOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const { browserResources, loading, refetch } = useBrowserResources();
  const { createBrowserResource, loading: creating } = useCreateBrowserResource();
  const { deleteBrowserResource } = useDeleteBrowserResource();
  const { canManage } = useCanManageWorkspaceFeatures();

  const filtered = useMemo(() => {
    const query = search.toLowerCase();
    return browserResources.filter((resource) => {
      if (!query) return true;
      return (
        resource.displayName.toLowerCase().includes(query) ||
        resource.id.toLowerCase().includes(query) ||
        (resource.currentAgentId?.toLowerCase().includes(query) ?? false)
      );
    });
  }, [browserResources, search]);
  const filteredIds = useMemo(
    () => filtered.map((resource) => resource.id),
    [filtered],
  );
  const selectedVisibleCount = filteredIds.filter((id) =>
    selectedIds.has(id),
  ).length;
  const allVisibleSelected =
    filteredIds.length > 0 && selectedVisibleCount === filteredIds.length;
  const someVisibleSelected = selectedVisibleCount > 0 && !allVisibleSelected;

  function toggleResource(resourceId: string, checked: boolean) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (checked) next.add(resourceId);
      else next.delete(resourceId);
      return next;
    });
  }

  function toggleVisibleResources(checked: boolean) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      for (const id of filteredIds) {
        if (checked) next.add(id);
        else next.delete(id);
      }
      return next;
    });
  }

  async function deleteSelectedResources() {
    if (!canManage) return;
    const ids = Array.from(selectedIds);
    await Promise.all(ids.map((id) => deleteBrowserResource(id)));
    setSelectedIds(new Set());
    refetch();
  }

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page="Browser"
        subtitle="Manage browser resources agents can mount into sessions."
        width="wide"
        action={
          <Button
            size="sm"
            disabled={!canManage}
            onClick={() => setCreateOpen(true)}
          >
            <PlusIcon className="size-4" />
            Create
          </Button>
        }
      />
      <PageContainer width="wide" className="flex flex-1 flex-col gap-4 pb-4">
        <div className="flex min-h-9 items-center justify-between gap-2">
          <SearchInput
            placeholder="Search browsers..."
            value={search}
            onChange={setSearch}
          />
          <TableSelectionToolbar selectedCount={selectedIds.size}>
            <Button
              variant="destructive"
              size="sm"
              disabled={!canManage}
              onClick={deleteSelectedResources}
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
                onCheckedChange={toggleVisibleResources}
              />
              <TableHead>ID</TableHead>
              <TableHead>Name</TableHead>
              <TableHead>Attached agent</TableHead>
              <TableHead>Created</TableHead>
              <TableHead>Updated</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filtered.map((resource) => (
              <TableRow
                key={resource.id}
                data-state={selectedIds.has(resource.id) ? "selected" : undefined}
                onClick={() => router.push(`/browser/${resource.id}`)}
                className="cursor-pointer"
              >
                <TableSelectionCell
                  checked={selectedIds.has(resource.id)}
                  aria-label={`Select ${resource.displayName}`}
                  onCheckedChange={(checked) =>
                    toggleResource(resource.id, checked)
                  }
                />
                <TableCell>{resource.id}</TableCell>
                <TableCell>{resource.displayName}</TableCell>
                <TableCell className="font-mono text-xs">
                  {resource.currentAgentId ?? "Not attached"}
                </TableCell>
                <TableCell>{formatDate(resource.createdAt)}</TableCell>
                <TableCell>{formatDate(resource.updatedAt)}</TableCell>
              </TableRow>
            ))}
            {!loading && filtered.length === 0 && (
              <TableRow>
                <TableCell colSpan={6} className="p-0">
                  <EmptyState message="No browser resources found." />
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </PageContainer>

      <ResourceCreateDialog
        open={createOpen}
        onOpenChange={setCreateOpen}
        title="Create browser"
        description="Create a browser resource that can be attached to an agent session."
        placeholder="Research Browser"
        defaultName="Browser"
        submitting={creating}
        onCreate={async (name) => {
          await createBrowserResource(name);
        }}
      />
    </>
  );
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Unknown" : date.toLocaleString();
}
