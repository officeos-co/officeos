"use client";

import { use } from "react";
import Link from "next/link";
import { notFound } from "next/navigation";
import { formatDistanceToNow } from "date-fns";
import {
  ArrowLeftIcon,
  ExternalLinkIcon,
  RefreshCwIcon,
} from "lucide-react";
import { PageContainer } from "@/components/page-container";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  type AtlasActivity,
  type AtlasConnection,
  parseJsonArray,
  useAtlasActivity,
  useAtlasConnection,
  useAtlasIndexJobs,
  useStartAtlasIndex,
} from "@/features/atlas";
import { cn } from "@/lib/utils";

const statusStyles: Record<string, string> = {
  NeedsAuth: "border-amber-200 bg-amber-50 text-amber-800",
  Indexing: "border-sky-200 bg-sky-50 text-sky-800",
  Ready: "border-emerald-200 bg-emerald-50 text-emerald-800",
  Failed: "border-red-200 bg-red-50 text-red-800",
};

export default function AtlasConnectorDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const { connection, loading } = useAtlasConnection(id, {
    pollInterval: 3000,
  });
  const { activity } = useAtlasActivity(id, { pollInterval: 5000 });
  const { jobs } = useAtlasIndexJobs(id, {
    limit: 10,
    pollInterval: 5000,
  });
  const { startIndex } = useStartAtlasIndex();

  if (!connection) {
    if (loading) return <ConnectorSkeleton />;
    return notFound();
  }

  return (
    <div className="flex min-h-screen flex-col">
      <div className="sticky top-0 z-10 bg-background">
        <PageContainer width="wide" className="border-b border-border">
          <div className="flex items-start justify-between gap-4 py-4">
            <div className="min-w-0">
              <Link
                href="/atlas/connectors"
                className="mb-2 inline-flex items-center gap-1 text-xs font-medium text-muted-foreground transition-colors hover:text-foreground"
              >
                <ArrowLeftIcon className="size-3" />
                Connectors
              </Link>
              <div className="flex items-center gap-2.5">
                <h1 className="truncate text-lg font-semibold">
                  {connection.displayName}
                </h1>
                <StatusBadge status={connection.status} />
              </div>
              <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-muted-foreground">
                <span className="font-mono">{connection.id}</span>
                <span>{parseJsonArray(connection.repositoriesJson).join(", ")}</span>
              </div>
            </div>
            <div className="flex shrink-0 items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                nativeButton={false}
                render={<Link href={`/atlas/history?connectionId=${connection.id}`} />}
              >
                History
                <ExternalLinkIcon className="size-3.5" />
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => startIndex(connection.id)}
              >
                <RefreshCwIcon className="size-3.5" />
                Re-index
              </Button>
            </div>
          </div>
        </PageContainer>
      </div>

      <PageContainer width="wide" className="flex flex-1 flex-col py-6">
        <ConnectorOverview connection={connection} activity={activity} jobs={jobs} />
      </PageContainer>
    </div>
  );
}

function ConnectorOverview({
  connection,
  activity,
  jobs,
}: {
  connection: AtlasConnection;
  activity: AtlasActivity[];
  jobs: Array<{
    id: string;
    status: string;
    recordsIndexed: number;
    error?: string | null;
    createdAt: string;
    completedAt?: string | null;
  }>;
}) {
  const totalRecords = connection.entityStatuses.reduce(
    (sum, entity) => sum + entity.recordCount,
    0,
  );
  const latestJob = jobs[0];
  const entities = parseJsonArray(connection.entitiesJson);

  return (
    <div className="space-y-6">
      <div className="grid gap-3 md:grid-cols-4">
        <Metric label="Status" value={formatStatus(connection.status)} />
        <Metric label="Entities" value={connection.entityStatuses.length} />
        <Metric label="Records" value={totalRecords} />
        <Metric
          label="Updated"
          value={formatDate(connection.updatedAt)}
          compact
        />
      </div>

      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_22rem]">
        <section>
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-sm font-semibold">Entity status</h2>
            {latestJob ? (
              <span className="text-xs text-muted-foreground">
                Latest job {formatStatus(latestJob.status)}
              </span>
            ) : null}
          </div>
          <div className="grid gap-3 md:grid-cols-2">
            {connection.entityStatuses.map((entity) => (
              <Link
                key={entity.entity}
                href={`/atlas/explorer?connectionId=${connection.id}&entity=${encodeURIComponent(entity.entity)}`}
                className="rounded-lg border border-border bg-card p-4 transition-colors hover:bg-muted/50"
              >
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <div className="font-mono text-sm">{entity.entity}</div>
                    <div className="mt-1 text-xs text-muted-foreground">
                      {entity.lastSyncedAt
                        ? `Synced ${formatDate(entity.lastSyncedAt)}`
                        : "Not synced yet"}
                    </div>
                  </div>
                  <StatusBadge status={entity.status} />
                </div>
                <div className="mt-4 text-2xl font-semibold">
                  {entity.recordCount}
                </div>
                {entity.error ? (
                  <div className="mt-2 truncate text-xs text-red-700">
                    {entity.error}
                  </div>
                ) : null}
              </Link>
            ))}
            {connection.entityStatuses.length === 0 &&
              entities.map((entity) => (
                <Link
                  key={entity}
                  href={`/atlas/explorer?connectionId=${connection.id}&entity=${encodeURIComponent(entity)}`}
                  className="rounded-lg border border-dashed border-border p-4 transition-colors hover:bg-muted/50"
                >
                  <div className="font-mono text-sm">{entity}</div>
                  <div className="mt-1 text-xs text-muted-foreground">
                    Waiting for first sync
                  </div>
                </Link>
              ))}
          </div>
        </section>

        <div className="space-y-6">
          <section>
            <div className="mb-3 flex items-center justify-between">
              <h2 className="text-sm font-semibold">Latest lifecycle activity</h2>
            </div>
            <ActivityList items={activity.slice(0, 6)} compact />
          </section>

          <section>
            <div className="mb-3 flex items-center justify-between">
              <h2 className="text-sm font-semibold">Configuration</h2>
            </div>
            <dl className="divide-y divide-border rounded-lg border border-border">
              <SettingRow label="Provider" value={connection.provider} />
              <SettingRow label="Workspace" value={connection.workspaceName} />
              <SettingRow
                label="Repositories"
                value={parseJsonArray(connection.repositoriesJson).join(", ")}
              />
              <SettingRow label="Entities" value={entities.join(", ")} />
              <SettingRow label="Created" value={formatDate(connection.createdAt)} />
              <SettingRow
                label="Last updated"
                value={formatDate(connection.updatedAt)}
              />
            </dl>
          </section>

          {connection.error ? (
            <section>
              <h2 className="text-sm font-semibold">Error</h2>
              <pre className="mt-3 whitespace-pre-wrap rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-800">
                {connection.error}
              </pre>
            </section>
          ) : null}
        </div>
      </div>
    </div>
  );
}

function ActivityList({
  items,
  compact = false,
}: {
  items: AtlasActivity[];
  compact?: boolean;
}) {
  if (items.length === 0) {
    return (
      <div className="rounded-lg border border-dashed border-border px-4 py-8 text-center text-sm text-muted-foreground">
        No activity yet.
      </div>
    );
  }

  return (
    <div className="rounded-lg border border-border">
      {items.map((item, index) => (
        <div
          key={item.id}
          className={cn(
            "grid gap-3 px-4 py-3",
            compact ? "grid-cols-[1rem_1fr]" : "grid-cols-[1rem_1fr_8rem]",
            index < items.length - 1 && "border-b border-border",
          )}
        >
          <span
            className={cn(
              "mt-1.5 size-2.5 rounded-full",
              item.success ? "bg-emerald-500" : "bg-red-500",
            )}
          />
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <span className="font-medium">{item.message}</span>
              {item.entity ? (
                <span className="rounded bg-muted px-1.5 py-0.5 font-mono text-[11px] text-muted-foreground">
                  {item.entity}
                </span>
              ) : null}
              <span className="rounded bg-muted px-1.5 py-0.5 font-mono text-[11px] text-muted-foreground">
                {item.type}
              </span>
            </div>
            {!compact && item.detailsJson !== "{}" ? (
              <pre className="mt-2 max-h-36 overflow-auto rounded-md bg-muted p-2 text-xs text-muted-foreground">
                {formatJson(item.detailsJson)}
              </pre>
            ) : null}
          </div>
          {!compact ? (
            <div className="text-right text-xs text-muted-foreground">
              {formatDate(item.createdAt)}
            </div>
          ) : null}
        </div>
      ))}
    </div>
  );
}

function Metric({
  label,
  value,
  compact,
}: {
  label: string;
  value: string | number;
  compact?: boolean;
}) {
  return (
    <div className="rounded-lg border border-border bg-card p-4">
      <div className="text-xs font-medium uppercase tracking-widest text-muted-foreground">
        {label}
      </div>
      <div
        className={cn(
          "mt-2 font-semibold",
          compact ? "text-base" : "text-2xl",
        )}
      >
        {value}
      </div>
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  return (
    <span
      className={cn(
        "inline-flex rounded-full border px-2.5 py-1 text-[10px] font-semibold uppercase tracking-widest",
        statusStyles[status] ?? "border-border bg-muted text-muted-foreground",
      )}
    >
      {formatStatus(status)}
    </span>
  );
}

function SettingRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="grid gap-2 px-4 py-3 text-sm md:grid-cols-[7rem_1fr]">
      <dt className="text-muted-foreground">{label}</dt>
      <dd className="break-words">{value || "None"}</dd>
    </div>
  );
}

function ConnectorSkeleton() {
  return (
    <PageContainer width="wide" className="space-y-4 py-6">
      <Skeleton className="h-16 w-full" />
      <Skeleton className="h-80 w-full" />
    </PageContainer>
  );
}

function formatDate(value: string) {
  try {
    return formatDistanceToNow(new Date(value), { addSuffix: true });
  } catch {
    return "Unknown";
  }
}

function formatStatus(value: string) {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}

function formatJson(value: string) {
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}
