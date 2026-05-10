"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { formatDistanceToNow } from "date-fns";
import { PlusIcon, RefreshCwIcon } from "lucide-react";
import { PageContainer } from "@/shell/page-container";
import { PageHeader } from "@/shell/page-header";
import { Button } from "@/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/ui/table";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/ui/tooltip";
import {
  AtlasConnectorDialog,
  type AtlasHistory,
  parseJsonArray,
  useAtlasConnections,
  useAtlasConnectorTypes,
  useCreateAtlasGitHubConnection,
  useAtlasHistory,
  useStartAtlasIndex,
} from "@/features/atlas";
import { cn } from "@/lib/utils";

const statusStyles: Record<
  string,
  { label: string; className: string; dotClassName: string }
> = {
  NeedsAuth: {
    label: "Needs auth",
    className: "border-amber-200 bg-amber-50 text-amber-800",
    dotClassName: "bg-amber-500",
  },
  Indexing: {
    label: "Indexing",
    className: "border-sky-200 bg-sky-50 text-sky-800",
    dotClassName: "bg-sky-500",
  },
  Ready: {
    label: "Ready",
    className: "border-emerald-200 bg-emerald-50 text-emerald-800",
    dotClassName: "bg-emerald-500",
  },
  Failed: {
    label: "Failed",
    className: "border-red-200 bg-red-50 text-red-800",
    dotClassName: "bg-red-500",
  },
};

export default function AtlasConnectorsPage() {
  const [dialogOpen, setDialogOpen] = useState(false);
  const { connections, loading, refetch } = useAtlasConnections({
    pollInterval: 3000,
  });
  const { connectorTypes } = useAtlasConnectorTypes();
  const { history } = useAtlasHistory(null, { pollInterval: 5000 });
  const { createConnection } = useCreateAtlasGitHubConnection();
  const { startIndex } = useStartAtlasIndex();
  const githubConnector = connectorTypes.find(
    (connector) => connector.provider === "github",
  );
  const historyByConnection = useMemo(() => {
    const grouped = new Map<string, AtlasHistory[]>();
    for (const item of history) {
      const items = grouped.get(item.connectionId);
      if (items) {
        if (items.length < 4) items.push(item);
      } else {
        grouped.set(item.connectionId, [item]);
      }
    }
    return grouped;
  }, [history]);

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
                          <Link
                            href={`/atlas/connectors/${connection.id}`}
                            className="font-medium transition-colors hover:text-primary hover:underline"
                          >
                            {connection.displayName}
                          </Link>
                          <div className="text-xs text-muted-foreground">
                            {repositories.join(", ") || "No repositories"}
                          </div>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>{formatDate(connection.createdAt)}</TableCell>
                    <TableCell>{formatDate(connection.updatedAt)}</TableCell>
                    <TableCell>
                      <ConnectionStatus
                        status={connection.status}
                        error={connection.error}
                      />
                    </TableCell>
                    <TableCell>
                      <HistoryLink
                        connectionId={connection.id}
                        items={historyByConnection.get(connection.id) ?? []}
                      />
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

      <AtlasConnectorDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        connectorType={githubConnector}
        oauthConfigured={Boolean(githubConnector?.oauthConfigured)}
        onSave={async (values) => {
          await createConnection(values);
          await refetch();
        }}
      />
    </>
  );
}

function ConnectionStatus({
  status,
  error,
}: {
  status: string;
  error?: string | null;
}) {
  const style = statusStyles[status] ?? {
    label: status,
    className: "border-border bg-muted text-muted-foreground",
    dotClassName: "bg-muted-foreground",
  };

  return (
    <div className="flex min-w-28 flex-col items-start gap-1">
      <span
        className={cn(
          "inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[10px] font-semibold uppercase tracking-widest",
          style.className,
        )}
      >
        <span className={cn("size-1.5 rounded-full", style.dotClassName)} />
        {style.label}
      </span>
      {error ? (
        <span className="max-w-40 truncate text-xs text-red-700" title={error}>
          {error}
        </span>
      ) : null}
    </div>
  );
}

function HistoryLink({
  connectionId,
  items,
}: {
  connectionId: string;
  items: AtlasHistory[];
}) {
  return (
    <TooltipProvider delay={700}>
      <Tooltip>
        <TooltipTrigger
          render={
            <Link
              href={`/atlas/history?connectionId=${connectionId}`}
              className="rounded-sm text-sm font-medium text-primary transition-colors hover:text-primary/80 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
            >
              History
            </Link>
          }
        />
        <TooltipContent
          side="bottom"
          align="start"
          sideOffset={8}
          className="block w-80 max-w-80 rounded-md border border-border bg-popover p-3 text-popover-foreground shadow-lg"
        >
          <span className="mb-2 block text-xs font-semibold uppercase tracking-widest text-muted-foreground">
            Recent requests
          </span>
          {items.length > 0 ? (
            <span className="block space-y-2">
              {items.map((item) => (
                <span
                  key={item.id}
                  className="block rounded-md border border-border bg-background px-3 py-2"
                >
                  <span className="flex items-center justify-between gap-3">
                    <span className="truncate font-mono text-xs">
                      {item.entity}.{item.action}
                    </span>
                    <span
                      className={cn(
                        "shrink-0 text-[10px] font-semibold uppercase tracking-widest",
                        item.success ? "text-emerald-700" : "text-red-700",
                      )}
                    >
                      {item.success ? "Success" : "Failed"}
                    </span>
                  </span>
                  <span className="mt-1 flex items-center justify-between gap-3 text-xs text-muted-foreground">
                    <span>{formatDate(item.createdAt)}</span>
                    <span>{item.durationMs}ms</span>
                  </span>
                  {item.error ? (
                    <span className="mt-1 block truncate text-xs text-red-700">
                      {item.error}
                    </span>
                  ) : null}
                </span>
              ))}
            </span>
          ) : (
            <span className="block rounded-md border border-dashed border-border px-3 py-4 text-center text-sm text-muted-foreground">
              No recent history.
            </span>
          )}
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}

function formatDate(value: string) {
  try {
    return formatDistanceToNow(new Date(value), { addSuffix: true });
  } catch {
    return "Unknown";
  }
}
