"use client";

import { use } from "react";
import Link from "next/link";
import { notFound, useSearchParams } from "next/navigation";
import { formatDistanceToNow } from "date-fns";
import {
  ArrowLeftIcon,
  ExternalLinkIcon,
  RefreshCwIcon,
  SearchIcon,
} from "lucide-react";
import { PageContainer } from "@/components/page-container";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  type AtlasActivity,
  type AtlasConnection,
  type AtlasIndexedRecord,
  parseJsonArray,
  useAtlasActivity,
  useAtlasConnection,
  useAtlasIndexedRecords,
  useAtlasIndexJobs,
  useStartAtlasIndex,
} from "@/features/atlas";
import { cn } from "@/lib/utils";

const TABS = [
  { key: "overview", label: "Overview" },
  { key: "activity", label: "Activity" },
  { key: "data", label: "Data" },
  { key: "settings", label: "Settings" },
] as const;

type TabKey = (typeof TABS)[number]["key"];

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
  const searchParams = useSearchParams();
  const tab = normalizeTab(searchParams.get("tab"));
  const queryEntity = searchParams.get("entity");
  const query = searchParams.get("q") ?? "";
  const cursor = searchParams.get("cursor");
  const selectedRecordId = searchParams.get("recordId");

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

  const entities = parseJsonArray(connection.entitiesJson);
  const entity = entities.includes(queryEntity ?? "")
    ? queryEntity!
    : entities[0] ?? "repositories";

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
            <Button
              variant="outline"
              size="sm"
              onClick={() => startIndex(connection.id)}
            >
              <RefreshCwIcon className="size-3.5" />
              Re-index
            </Button>
          </div>
          <div className="-mb-px flex">
            {TABS.map((item) => (
              <Link
                key={item.key}
                href={`/atlas/connectors/${id}?tab=${item.key}`}
                className={cn(
                  "border-b-2 px-4 py-2.5 text-sm font-medium transition-colors",
                  tab === item.key
                    ? "border-foreground text-foreground"
                    : "border-transparent text-muted-foreground hover:text-foreground",
                )}
              >
                {item.label}
              </Link>
            ))}
          </div>
        </PageContainer>
      </div>

      <PageContainer width="wide" className="flex flex-1 flex-col py-6">
        {tab === "overview" && (
          <OverviewTab connection={connection} activity={activity} jobs={jobs} />
        )}
        {tab === "activity" && <ActivityTab activity={activity} />}
        {tab === "data" && (
          <DataTab
            connection={connection}
            entity={entity}
            query={query}
            cursor={cursor}
            selectedRecordId={selectedRecordId}
          />
        )}
        {tab === "settings" && <SettingsTab connection={connection} />}
      </PageContainer>
    </div>
  );
}

function OverviewTab({
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

      <div className="grid gap-6 lg:grid-cols-[1fr_22rem]">
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
              <div
                key={entity.entity}
                className="rounded-lg border border-border bg-card p-4"
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
              </div>
            ))}
          </div>
        </section>

        <section>
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-sm font-semibold">Latest activity</h2>
            <Link
              href={`/atlas/connectors/${connection.id}?tab=activity`}
              className="text-xs font-medium text-primary hover:underline"
            >
              View all
            </Link>
          </div>
          <ActivityList items={activity.slice(0, 6)} compact />
        </section>
      </div>
    </div>
  );
}

function ActivityTab({ activity }: { activity: AtlasActivity[] }) {
  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-sm font-semibold">Connector activity</h2>
        <p className="mt-1 text-sm text-muted-foreground">
          Lifecycle events emitted while Atlas connects, validates, and indexes.
        </p>
      </div>
      <ActivityList items={activity} />
    </div>
  );
}

function DataTab({
  connection,
  entity,
  query,
  cursor,
  selectedRecordId,
}: {
  connection: AtlasConnection;
  entity: string;
  query: string;
  cursor?: string | null;
  selectedRecordId?: string | null;
}) {
  const limit = 20;
  const { page, loading } = useAtlasIndexedRecords({
    connectionId: connection.id,
    entity,
    query,
    cursor,
    limit,
  });
  const selected =
    page.records.find((record) => record.id === selectedRecordId) ??
    page.records[0] ??
    null;
  const currentOffset = Math.max(Number.parseInt(cursor ?? "0", 10) || 0, 0);
  const previousOffset = Math.max(currentOffset - limit, 0);

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex flex-wrap gap-1">
          {parseJsonArray(connection.entitiesJson).map((item) => (
            <Link
              key={item}
              href={`/atlas/connectors/${connection.id}?tab=data&entity=${encodeURIComponent(item)}`}
              className={cn(
                "rounded-md border px-3 py-1.5 text-xs font-medium transition-colors",
                item === entity
                  ? "border-foreground bg-foreground text-background"
                  : "border-border text-muted-foreground hover:text-foreground",
              )}
            >
              {item.replaceAll("_", " ")}
            </Link>
          ))}
        </div>
        <form
          action={`/atlas/connectors/${connection.id}`}
          className="relative w-full lg:w-80"
        >
          <input type="hidden" name="tab" value="data" />
          <input type="hidden" name="entity" value={entity} />
          <SearchIcon className="absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            name="q"
            defaultValue={query}
            placeholder={`Search ${entity.replaceAll("_", " ")}`}
            className="pl-8"
          />
        </form>
      </div>

      <div className="grid min-h-[34rem] gap-4 lg:grid-cols-[minmax(0,1fr)_24rem]">
        <div className="overflow-hidden rounded-lg border border-border">
          <div className="grid grid-cols-[1fr_8rem_8rem] border-b border-border bg-muted/40 px-3 py-2 text-xs font-medium uppercase tracking-widest text-muted-foreground">
            <span>Record</span>
            <span>External</span>
            <span>Indexed</span>
          </div>
          <div className="divide-y divide-border">
            {page.records.map((record) => (
              <Link
                key={record.id}
                href={recordUrl(connection.id, entity, query, cursor, record.id)}
                className={cn(
                  "grid grid-cols-[1fr_8rem_8rem] gap-3 px-3 py-3 text-sm transition-colors hover:bg-muted/60",
                  selected?.id === record.id && "bg-muted",
                )}
              >
                <span className="min-w-0">
                  <span className="block truncate font-medium">
                    {record.title}
                  </span>
                  <span className="mt-0.5 block truncate font-mono text-xs text-muted-foreground">
                    {summarizeRecord(entity, record)}
                  </span>
                </span>
                <span className="text-xs text-muted-foreground">
                  {record.externalUpdatedAt
                    ? formatDate(record.externalUpdatedAt)
                    : "Unknown"}
                </span>
                <span className="text-xs text-muted-foreground">
                  {formatDate(record.updatedAt)}
                </span>
              </Link>
            ))}
            {!loading && page.records.length === 0 && (
              <div className="px-3 py-12 text-center text-sm text-muted-foreground">
                No indexed records found.
              </div>
            )}
            {loading && page.records.length === 0 && (
              <div className="space-y-2 p-3">
                <Skeleton className="h-12 w-full" />
                <Skeleton className="h-12 w-full" />
                <Skeleton className="h-12 w-full" />
              </div>
            )}
          </div>
          <div className="flex items-center justify-between border-t border-border px-3 py-2">
            <div className="text-xs text-muted-foreground">
              Showing {currentOffset + 1}-{currentOffset + page.records.length}
            </div>
            <div className="flex gap-2">
              {currentOffset > 0 ? (
                <Link
                  href={dataUrl(connection.id, entity, query, previousOffset)}
                  className="rounded-md border border-border px-2.5 py-1.5 text-xs font-medium hover:bg-muted"
                >
                  Previous
                </Link>
              ) : null}
              {page.hasMore && page.cursor ? (
                <Link
                  href={dataUrl(connection.id, entity, query, page.cursor)}
                  className="rounded-md border border-border px-2.5 py-1.5 text-xs font-medium hover:bg-muted"
                >
                  Next
                </Link>
              ) : null}
            </div>
          </div>
        </div>

        <RecordPreview record={selected} />
      </div>
    </div>
  );
}

function SettingsTab({ connection }: { connection: AtlasConnection }) {
  return (
    <div className="max-w-3xl space-y-6">
      <section>
        <h2 className="text-sm font-semibold">Configuration</h2>
        <dl className="mt-3 divide-y divide-border rounded-lg border border-border">
          <SettingRow label="Provider" value={connection.provider} />
          <SettingRow label="Workspace" value={connection.workspaceName} />
          <SettingRow label="Repositories" value={parseJsonArray(connection.repositoriesJson).join(", ")} />
          <SettingRow label="Entities" value={parseJsonArray(connection.entitiesJson).join(", ")} />
          <SettingRow label="Created" value={formatDate(connection.createdAt)} />
          <SettingRow label="Last updated" value={formatDate(connection.updatedAt)} />
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

function RecordPreview({ record }: { record: AtlasIndexedRecord | null }) {
  if (!record) {
    return (
      <aside className="rounded-lg border border-dashed border-border px-4 py-8 text-center text-sm text-muted-foreground">
        Select a record to preview it.
      </aside>
    );
  }

  const raw = parseJsonObject(record.rawJson);
  const htmlUrl = stringValue(raw.html_url);

  return (
    <aside className="rounded-lg border border-border bg-card">
      <div className="border-b border-border p-4">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <h2 className="truncate text-sm font-semibold">{record.title}</h2>
            <div className="mt-1 font-mono text-xs text-muted-foreground">
              {record.entity}
            </div>
          </div>
          {htmlUrl ? (
            <a
              href={htmlUrl}
              target="_blank"
              rel="noreferrer"
              className="text-muted-foreground transition-colors hover:text-foreground"
            >
              <ExternalLinkIcon className="size-4" />
            </a>
          ) : null}
        </div>
      </div>
      <div className="space-y-4 p-4">
        <dl className="space-y-2 text-sm">
          <PreviewRow label="External ID" value={record.externalId} mono />
          <PreviewRow
            label="External updated"
            value={
              record.externalUpdatedAt
                ? formatDate(record.externalUpdatedAt)
                : "Unknown"
            }
          />
          <PreviewRow label="Indexed" value={formatDate(record.updatedAt)} />
          <PreviewRow
            label="State"
            value={stringValue(raw.state) ?? stringValue(raw.default_branch)}
          />
          <PreviewRow
            label="Author"
            value={
              stringValue(objectValue(raw.user)?.login) ??
              stringValue(objectValue(raw.author)?.login)
            }
          />
        </dl>
        <div>
          <div className="mb-2 text-xs font-semibold uppercase tracking-widest text-muted-foreground">
            Search text
          </div>
          <div className="max-h-32 overflow-auto rounded-md bg-muted p-2 text-xs text-muted-foreground">
            {record.searchText}
          </div>
        </div>
        <div>
          <div className="mb-2 text-xs font-semibold uppercase tracking-widest text-muted-foreground">
            Raw JSON
          </div>
          <pre className="max-h-72 overflow-auto rounded-md bg-muted p-2 text-xs text-muted-foreground">
            {formatJson(record.rawJson)}
          </pre>
        </div>
      </div>
    </aside>
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
    <div className="grid gap-2 px-4 py-3 text-sm md:grid-cols-[10rem_1fr]">
      <dt className="text-muted-foreground">{label}</dt>
      <dd className="break-words">{value || "None"}</dd>
    </div>
  );
}

function PreviewRow({
  label,
  value,
  mono,
}: {
  label: string;
  value?: string | null;
  mono?: boolean;
}) {
  if (!value) return null;
  return (
    <div className="grid grid-cols-[7rem_1fr] gap-3">
      <dt className="text-muted-foreground">{label}</dt>
      <dd className={cn("min-w-0 truncate", mono && "font-mono text-xs")}>
        {value}
      </dd>
    </div>
  );
}

function ConnectorSkeleton() {
  return (
    <PageContainer width="wide" className="space-y-4 py-6">
      <Skeleton className="h-16 w-full" />
      <Skeleton className="h-10 w-80" />
      <Skeleton className="h-80 w-full" />
    </PageContainer>
  );
}

function normalizeTab(tab: string | null): TabKey {
  return TABS.some((item) => item.key === tab) ? (tab as TabKey) : "overview";
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

function dataUrl(
  connectionId: string,
  entity: string,
  query: string,
  cursor: string | number,
) {
  const params = new URLSearchParams({ tab: "data", entity });
  if (query) params.set("q", query);
  if (Number(cursor) > 0) params.set("cursor", String(cursor));
  return `/atlas/connectors/${connectionId}?${params.toString()}`;
}

function recordUrl(
  connectionId: string,
  entity: string,
  query: string,
  cursor: string | null | undefined,
  recordId: string,
) {
  const params = new URLSearchParams({ tab: "data", entity, recordId });
  if (query) params.set("q", query);
  if (cursor) params.set("cursor", cursor);
  return `/atlas/connectors/${connectionId}?${params.toString()}`;
}

function parseJsonObject(value: string): Record<string, unknown> {
  try {
    const parsed = JSON.parse(value);
    return parsed && typeof parsed === "object" && !Array.isArray(parsed)
      ? parsed
      : {};
  } catch {
    return {};
  }
}

function formatJson(value: string) {
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}

function stringValue(value: unknown): string | null {
  if (value === null || value === undefined) return null;
  if (typeof value === "string") return value;
  if (typeof value === "number" || typeof value === "boolean") {
    return String(value);
  }
  return null;
}

function objectValue(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;
}

function summarizeRecord(entity: string, record: AtlasIndexedRecord) {
  const raw = parseJsonObject(record.rawJson);
  if (entity === "commits") {
    return stringValue(raw.sha) ?? record.externalId;
  }
  if (entity === "repositories") {
    return stringValue(raw.full_name) ?? record.externalId;
  }
  const number = stringValue(raw.number);
  const state = stringValue(raw.state);
  return [number ? `#${number}` : null, state].filter(Boolean).join(" · ");
}
