"use client";

import Link from "next/link";
import { useState } from "react";
import { formatDistanceToNow } from "date-fns";
import { PlusIcon, RefreshCwIcon } from "lucide-react";
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
  GitHubConnectionDialog,
  parseJsonArray,
  useAtlasConnections,
  useAtlasConnectorTypes,
  useCreateAtlasGitHubConnection,
  useStartAtlasIndex,
} from "@/features/atlas";
import { cn } from "@/lib/utils";

const statusLabel: Record<string, string> = {
  NeedsAuth: "Needs auth",
  Indexing: "Indexing",
  Ready: "Ready",
  Failed: "Failed",
};

export default function AtlasConnectorsPage() {
  const [dialogOpen, setDialogOpen] = useState(false);
  const { connections, loading, refetch } = useAtlasConnections();
  const { connectorTypes } = useAtlasConnectorTypes();
  const { createConnection } = useCreateAtlasGitHubConnection();
  const { startIndex } = useStartAtlasIndex();
  const githubConnector = connectorTypes.find(
    (connector) => connector.provider === "github",
  );

  return (
    <>
      <PageHeader
        group="Atlas"
        page="Connectors"
        subtitle="Manage Atlas-compatible data connectors."
        width="thin"
        action={
          <Button size="sm" onClick={() => setDialogOpen(true)}>
            <PlusIcon className="size-4" />
            Add connector
          </Button>
        }
      />
      <PageContainer width="thin" className="flex flex-1 flex-col pb-4">
        <div className="min-h-0 overflow-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Connector</TableHead>
                <TableHead>Created</TableHead>
                <TableHead>Last updated</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>History</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {connections.map((connection) => {
                const repositories = parseJsonArray(
                  connection.repositoriesJson,
                );
                return (
                  <TableRow key={connection.id}>
                    <TableCell>
                      <div className="flex items-center gap-3">
                        <div className="flex size-9 items-center justify-center rounded-md bg-foreground text-background">
                          {githubConnector?.logo ? (
                            <span
                              className="size-5 [&>svg]:size-5 [&>svg]:fill-current"
                              dangerouslySetInnerHTML={{
                                __html: githubConnector.logo,
                              }}
                            />
                          ) : null}
                        </div>
                        <div>
                          <div className="font-medium">
                            {connection.displayName}
                          </div>
                          <div className="text-xs text-muted-foreground">
                            {repositories.join(", ") || "No repositories"}
                          </div>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>{formatDate(connection.createdAt)}</TableCell>
                    <TableCell>{formatDate(connection.updatedAt)}</TableCell>
                    <TableCell>
                      <span
                        className={cn(
                          "inline-flex rounded-full px-2.5 py-1 text-[10px] font-semibold uppercase tracking-widest",
                          connection.status === "Ready" &&
                            "bg-emerald-100 text-emerald-700",
                          connection.status === "Indexing" &&
                            "bg-blue-100 text-blue-700",
                          connection.status === "NeedsAuth" &&
                            "bg-amber-100 text-amber-700",
                          connection.status === "Failed" &&
                            "bg-red-100 text-red-700",
                        )}
                      >
                        {statusLabel[connection.status] ?? connection.status}
                      </span>
                    </TableCell>
                    <TableCell>
                      <Link
                        href={`/atlas/history?connectionId=${connection.id}`}
                        className="text-sm font-medium text-primary hover:underline"
                      >
                        History
                      </Link>
                    </TableCell>
                    <TableCell className="text-right">
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        onClick={() => startIndex(connection.id)}
                        title="Re-index"
                      >
                        <RefreshCwIcon className="size-4" />
                      </Button>
                    </TableCell>
                  </TableRow>
                );
              })}
              {!loading && connections.length === 0 && (
                <TableRow>
                  <TableCell
                    colSpan={6}
                    className="py-10 text-center text-muted-foreground"
                  >
                    No Atlas connectors yet.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
      </PageContainer>

      <GitHubConnectionDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        connectorType={githubConnector}
        onSave={async (values) => {
          await createConnection(values);
          await refetch();
        }}
      />
    </>
  );
}

function formatDate(value: string) {
  try {
    return formatDistanceToNow(new Date(value), { addSuffix: true });
  } catch {
    return "Unknown";
  }
}
