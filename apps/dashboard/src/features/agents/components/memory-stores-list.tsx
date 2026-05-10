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
  useCreateMemoryStore,
  useDeleteMemoryStore,
  useMemoryStores,
} from "../api/useAgentResources";
import { ResourceCreateDialog } from "./resource-create-dialog";

export function MemoryStoresList() {
  const router = useRouter();
  const [createOpen, setCreateOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const { memoryStores, loading, refetch } = useMemoryStores();
  const { createMemoryStore, loading: creating } = useCreateMemoryStore();
  const { deleteMemoryStore } = useDeleteMemoryStore();

  const filtered = useMemo(() => {
    const query = search.toLowerCase();
    return memoryStores.filter((store) => {
      if (!query) return true;
      return (
        store.displayName.toLowerCase().includes(query) ||
        store.id.toLowerCase().includes(query)
      );
    });
  }, [memoryStores, search]);
  const filteredIds = useMemo(() => filtered.map((store) => store.id), [filtered]);
  const selectedVisibleCount = filteredIds.filter((id) =>
    selectedIds.has(id),
  ).length;
  const allVisibleSelected =
    filteredIds.length > 0 && selectedVisibleCount === filteredIds.length;
  const someVisibleSelected = selectedVisibleCount > 0 && !allVisibleSelected;

  function toggleStore(storeId: string, checked: boolean) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (checked) next.add(storeId);
      else next.delete(storeId);
      return next;
    });
  }

  function toggleVisibleStores(checked: boolean) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      for (const id of filteredIds) {
        if (checked) next.add(id);
        else next.delete(id);
      }
      return next;
    });
  }

  async function deleteSelectedStores() {
    const ids = Array.from(selectedIds);
    await Promise.all(ids.map((id) => deleteMemoryStore(id)));
    setSelectedIds(new Set());
    refetch();
  }

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page="Memory Store"
        subtitle="Manage memory stores agents can mount into sessions."
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
            placeholder="Search memory stores..."
            value={search}
            onChange={setSearch}
          />
          <TableSelectionToolbar selectedCount={selectedIds.size}>
            <Button variant="destructive" size="sm" onClick={deleteSelectedStores}>
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
                onCheckedChange={toggleVisibleStores}
              />
              <TableHead>ID</TableHead>
              <TableHead>Name</TableHead>
              <TableHead>Created</TableHead>
              <TableHead>Updated</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filtered.map((store) => (
              <TableRow
                key={store.id}
                data-state={selectedIds.has(store.id) ? "selected" : undefined}
                onClick={() => router.push(`/memory-stores/${store.id}`)}
                className="cursor-pointer"
              >
                <TableSelectionCell
                  checked={selectedIds.has(store.id)}
                  aria-label={`Select ${store.displayName}`}
                  onCheckedChange={(checked) => toggleStore(store.id, checked)}
                />
                <TableCell>{store.id}</TableCell>
                <TableCell>{store.displayName}</TableCell>
                <TableCell>{formatDate(store.createdAt)}</TableCell>
                <TableCell>{formatDate(store.updatedAt)}</TableCell>
              </TableRow>
            ))}
            {!loading && filtered.length === 0 && (
              <TableRow>
                <TableCell colSpan={5} className="p-0">
                  <EmptyState message="No memory stores found." />
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </PageContainer>

      <ResourceCreateDialog
        open={createOpen}
        onOpenChange={setCreateOpen}
        title="Create memory store"
        description="Create a memory store that can be attached to an agent session."
        placeholder="Company Knowledge"
        defaultName="Memory Store"
        submitting={creating}
        onCreate={async (name) => {
          await createMemoryStore(name);
        }}
      />
    </>
  );
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Unknown" : date.toLocaleString();
}
