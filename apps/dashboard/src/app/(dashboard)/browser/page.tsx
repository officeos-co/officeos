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
  useBrowserResources,
  useCreateBrowserResource,
} from "@/features/agents";

export default function BrowserResourcesPage() {
  const [createOpen, setCreateOpen] = useState(false);
  const { browserResources, loading } = useBrowserResources();
  const { createBrowserResource, loading: creating } = useCreateBrowserResource();

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page="Browser"
        subtitle="Manage browser resources agents can mount into sessions."
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
                <TableHead>Attached agent</TableHead>
                <TableHead>Created</TableHead>
                <TableHead>Updated</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {browserResources.map((resource) => (
                <TableRow key={resource.id}>
                  <TableCell>
                    <Link
                      href={`/browser/${resource.id}`}
                      className="font-medium hover:underline"
                    >
                      {resource.displayName}
                    </Link>
                  </TableCell>
                  <TableCell className="font-mono text-xs">
                    {resource.currentAgentId ?? "Not attached"}
                  </TableCell>
                  <TableCell>{formatDate(resource.createdAt)}</TableCell>
                  <TableCell>{formatDate(resource.updatedAt)}</TableCell>
                </TableRow>
              ))}
              {!loading && browserResources.length === 0 && (
                <TableRow>
                  <TableCell
                    colSpan={4}
                    className="py-10 text-center text-muted-foreground"
                  >
                    No browser resources yet.
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
