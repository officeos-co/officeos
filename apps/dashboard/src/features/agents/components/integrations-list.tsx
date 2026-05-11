"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import {
  CheckCircle2Icon,
  DatabaseIcon,
  PlusIcon,
  Trash2Icon,
} from "lucide-react";
import { PageHeader } from "@/shell/page-header";
import {
  PageContainer,
  getDialogWidthClassName,
} from "@/shell/page-container";
import { Button } from "@/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/ui/dialog";
import { EmptyState } from "@/ui/empty-state";
import { SearchInput } from "@/ui/search-input";
import { Skeleton } from "@/ui/skeleton";
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
  sortIntegrations,
  useDeleteIntegration,
  useIntegrationCatalog,
  useIntegrations,
  useSaveIntegrationCredential,
} from "../api/useIntegrations";
import { useIntegrationConnections } from "@/features/atlas";
import { canEditWorkspace, useWorkspaces } from "@/features/manage";
import type { McpServer } from "../data/integrations";
import { ConnectorDirectoryDialog } from "./connector-directory-dialog";
import { CustomMcpJsonEditor } from "./custom-mcp-json-editor";

export function IntegrationsList() {
  const router = useRouter();
  const { integrations, loading } = useIntegrations();
  const { integrations: catalogIntegrations } = useIntegrationCatalog();
  const { currentWorkspace } = useWorkspaces();
  const { connections } = useIntegrationConnections({ pollInterval: 5000 });
  const setCredentials = useSaveIntegrationCredential();
  const deleteIntegration = useDeleteIntegration();
  const [search, setSearch] = useState("");
  const [selectedNames, setSelectedNames] = useState<Set<string>>(new Set());
  const [directoryOpen, setDirectoryOpen] = useState(false);
  const [customMcpOpen, setCustomMcpOpen] = useState(false);

  const sorted = useMemo(
    () => sortIntegrations(integrations),
    [integrations],
  );
  const configured = sorted;
  const filtered = useMemo(() => {
    const query = search.toLowerCase();
    return configured.filter((server) => {
      if (!query) return true;
      return (
        server.name.toLowerCase().includes(query) ||
        server.title.toLowerCase().includes(query) ||
        server.subtitle.toLowerCase().includes(query) ||
        server.category.toLowerCase().includes(query)
      );
    });
  }, [configured, search]);
  const filteredNames = useMemo(
    () => filtered.map((server) => server.name),
    [filtered],
  );
  const selectedVisibleCount = filteredNames.filter((name) =>
    selectedNames.has(name),
  ).length;
  const allVisibleSelected =
    filteredNames.length > 0 && selectedVisibleCount === filteredNames.length;
  const someVisibleSelected = selectedVisibleCount > 0 && !allVisibleSelected;
  const selectedRemovableNames = configured
    .filter((server) => selectedNames.has(server.name) && !server.isBuiltin)
    .map((server) => server.name);
  const canManageWorkspaceFeatures =
    currentWorkspace?.ownerKind === "personal" ||
    canEditWorkspace(currentWorkspace?.role);

  function toggleConnector(name: string, checked: boolean) {
    setSelectedNames((prev) => {
      const next = new Set(prev);
      if (checked) next.add(name);
      else next.delete(name);
      return next;
    });
  }

  function toggleVisibleConnectors(checked: boolean) {
    setSelectedNames((prev) => {
      const next = new Set(prev);
      for (const name of filteredNames) {
        if (checked) next.add(name);
        else next.delete(name);
      }
      return next;
    });
  }

  async function removeSelectedConnectors() {
    if (!canManageWorkspaceFeatures) return;
    await Promise.all(selectedRemovableNames.map((name) => deleteIntegration(name)));
    setSelectedNames(new Set());
  }

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page="Integrations"
        subtitle={
          currentWorkspace
            ? `Configure integrations for ${currentWorkspace.name}.`
            : "Configure integrations for the current workspace."
        }
        width="wide"
        action={
          <div className="flex items-center gap-2">
            <Button
              size="sm"
              disabled={!canManageWorkspaceFeatures}
              onClick={() => setDirectoryOpen(true)}
            >
              <PlusIcon className="size-4" />
              Add integration
            </Button>
          </div>
        }
      />
      <PageContainer width="wide" className="flex flex-1 flex-col gap-4 pb-4">
        <div className="flex min-h-9 items-center justify-between gap-2">
          <SearchInput
            placeholder="Search integrations..."
            value={search}
            onChange={setSearch}
          />
          <TableSelectionToolbar selectedCount={selectedNames.size}>
            <Button
              variant="destructive"
              size="sm"
              disabled={
                !canManageWorkspaceFeatures ||
                selectedRemovableNames.length === 0
              }
              onClick={removeSelectedConnectors}
            >
              <Trash2Icon className="size-4" />
              Remove
            </Button>
          </TableSelectionToolbar>
        </div>

        <Table>
          <TableHeader>
            <TableRow>
              <TableSelectionHead
                checked={allVisibleSelected}
                indeterminate={someVisibleSelected}
                onCheckedChange={toggleVisibleConnectors}
              />
              <TableHead className="w-12" />
              <TableHead>Name</TableHead>
              <TableHead>Category</TableHead>
              <TableHead>Capabilities</TableHead>
              <TableHead>Configuration</TableHead>
              <TableHead>Status</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading &&
              configured.length === 0 &&
              Array.from({ length: 6 }).map((_, index) => (
                <TableRow key={`connector-skeleton-${index}`}>
                  <TableCell className="w-10 px-3">
                    <Skeleton className="size-4 rounded" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="size-8 rounded-lg" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-36" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-24" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-12" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-24" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-5 w-20 rounded-full" />
                  </TableCell>
                </TableRow>
              ))}
            {filtered.map((server) => {
              const indexState = getIndexState(server, connections);

              return (
                <TableRow
                  key={server.name}
                  data-state={
                    selectedNames.has(server.name) ? "selected" : undefined
                  }
                  onClick={() => router.push(`/integrations/${server.name}`)}
                  className="cursor-pointer"
                >
                  <TableSelectionCell
                    checked={selectedNames.has(server.name)}
                    aria-label={`Select ${server.title}`}
                    onCheckedChange={(checked) =>
                      toggleConnector(server.name, checked)
                    }
                  />
                  <TableCell>
                    <span
                      className="flex size-8 items-center justify-center rounded-lg border border-border bg-muted/40 [&>img]:size-5 [&>img]:object-contain [&>svg]:size-5"
                      dangerouslySetInnerHTML={{ __html: server.logo }}
                    />
                  </TableCell>
                  <TableCell>
                    <span className="font-medium">{server.title}</span>
                    {server.subtitle && (
                      <div className="text-xs text-muted-foreground">
                        {server.subtitle}
                      </div>
                    )}
                  </TableCell>
                  <TableCell>{server.category || "Integration"}</TableCell>
                  <TableCell>
                    <div className="flex flex-wrap gap-1.5">
                      <span className="rounded bg-muted px-1.5 py-0.5 text-xs text-muted-foreground">
                        {server.tools.length > 0
                          ? `${server.tools.length} tools`
                          : "No tools"}
                      </span>
                      {server.isIndexable ? (
                        <span className="inline-flex items-center gap-1 rounded bg-muted px-1.5 py-0.5 text-xs text-muted-foreground">
                          <DatabaseIcon className="size-3" />
                          Indexed data
                        </span>
                      ) : null}
                    </div>
                  </TableCell>
                  <TableCell>
                    <span className="text-sm text-muted-foreground">
                      {server.isIndexable
                        ? "Tool policies and data mode"
                        : "Tool policies"}
                    </span>
                  </TableCell>
                  <TableCell>
                    <div className="flex flex-wrap gap-1.5">
                      <span className="inline-flex items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-700">
                        <CheckCircle2Icon className="size-3" />
                        Connected
                      </span>
                      {server.isIndexable ? (
                        <span className="inline-flex items-center gap-1 rounded-full bg-sky-100 px-2 py-0.5 text-xs font-medium text-sky-700">
                          <DatabaseIcon className="size-3" />
                          {indexState.label}
                        </span>
                      ) : null}
                    </div>
                  </TableCell>
                </TableRow>
              );
            })}
            {!loading && filtered.length === 0 && (
              <TableRow>
                <TableCell colSpan={7} className="p-0">
                  <EmptyState message="No integrations found." />
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </PageContainer>

      <ConnectorDirectoryDialog
        open={directoryOpen}
        onOpenChange={setDirectoryOpen}
        integrations={sortIntegrations(catalogIntegrations)}
        onSaveCredential={async (server, values) => {
          await setCredentials(server.name, values);
        }}
        onAddCustomMcp={() => setCustomMcpOpen(true)}
      />

      <Dialog open={customMcpOpen} onOpenChange={setCustomMcpOpen}>
        <DialogContent
          className={getDialogWidthClassName(
            "thin",
            "flex h-[min(760px,calc(100vh-96px))] flex-col overflow-hidden p-6",
          )}
        >
          <DialogHeader>
            <DialogTitle>Custom MCP integrations</DialogTitle>
            <DialogDescription>
              Import custom MCP server definitions from JSON.
            </DialogDescription>
          </DialogHeader>
          <div className="mt-4 min-h-0 flex-1">
            <CustomMcpJsonEditor servers={sorted} loading={loading} />
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}

function getIndexState(
  integration: Pick<McpServer, "provider" | "name">,
  connections: Array<{
    provider: string;
    status: string;
    entityStatuses: Array<{ recordCount: number }>;
  }>,
) {
  const matching = connections.filter((connection) =>
    providerMatches(connection.provider, integration.provider || integration.name),
  );
  const records = matching.reduce(
    (sum, connection) =>
      sum +
      connection.entityStatuses.reduce(
        (entitySum, entity) => entitySum + entity.recordCount,
        0,
      ),
    0,
  );
  if (matching.some((connection) => connection.status === "Indexing")) {
    return { label: "Indexing" };
  }
  if (matching.some((connection) => connection.status === "Ready") || records > 0) {
    return { label: "Indexed" };
  }
  if (matching.some((connection) => connection.status === "Failed")) {
    return { label: "Index failed" };
  }
  return { label: "Indexable" };
}

function providerMatches(left: string, right: string) {
  const normalize = (value: string) =>
    value.toLowerCase().replace(/[^a-z0-9]/g, "");
  return normalize(left) === normalize(right);
}
