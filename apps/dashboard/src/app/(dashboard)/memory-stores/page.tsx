"use client";

import Link from "next/link";
import { useState } from "react";
import { PlusIcon } from "lucide-react";
import { PageContainer } from "@/components/page-container";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  ResourceCreateDialog,
  useCreateMemoryStore,
  useMemoryStores,
} from "@/features/agents";

export default function MemoryStoresPage() {
  const [createOpen, setCreateOpen] = useState(false);
  const { memoryStores, loading } = useMemoryStores();
  const { createMemoryStore, loading: creating } = useCreateMemoryStore();

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page="Memory Store"
        subtitle="Manage memory stores agents can mount into sessions."
        width="thin"
        action={
          <Button size="sm" onClick={() => setCreateOpen(true)}>
            <PlusIcon className="size-4" />
            Create
          </Button>
        }
      />
      <PageContainer width="thin" className="flex flex-1 flex-col pb-4">
        <div className="min-h-0 overflow-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Created</TableHead>
                <TableHead>Updated</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {memoryStores.map((store) => (
                <TableRow key={store.id}>
                  <TableCell>
                    <Link
                      href={`/memory-stores/${store.id}`}
                      className="font-medium hover:underline"
                    >
                      {store.displayName}
                    </Link>
                  </TableCell>
                  <TableCell>{formatDate(store.createdAt)}</TableCell>
                  <TableCell>{formatDate(store.updatedAt)}</TableCell>
                </TableRow>
              ))}
              {!loading && memoryStores.length === 0 && (
                <TableRow>
                  <TableCell
                    colSpan={3}
                    className="py-10 text-center text-muted-foreground"
                  >
                    No memory stores yet.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
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
