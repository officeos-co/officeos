"use client";

import { use } from "react";
import Link from "next/link";
import { notFound } from "next/navigation";
import { formatDistanceToNow } from "date-fns";
import { ArrowLeftIcon, ExternalLinkIcon } from "lucide-react";
import { PageContainer } from "@/components/page-container";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  type AtlasIndexedRecord,
  useAtlasConnection,
  useAtlasIndexedRecord,
} from "@/features/atlas";

export default function AtlasExplorerRecordPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const { record, loading } = useAtlasIndexedRecord(id);
  const { connection } = useAtlasConnection(record?.connectionId);

  if (!record) {
    if (loading) return <RecordSkeleton />;
    return notFound();
  }

  const raw = parseJsonObject(record.rawJson);
  const htmlUrl = stringValue(raw.html_url);

  return (
    <>
      <PageHeader
        group="Atlas"
        page={record.title || "Indexed record"}
        subtitle={`${connection?.displayName ?? "Indexed data"} / ${record.entity}`}
        width="wide"
        action={
          <div className="flex items-center gap-2">
            {htmlUrl ? (
              <Button
                size="sm"
                variant="outline"
                nativeButton={false}
                render={<a href={htmlUrl} target="_blank" rel="noreferrer" />}
              >
                Source
                <ExternalLinkIcon className="size-3.5" />
              </Button>
            ) : null}
            <Button
              size="sm"
              variant="outline"
              nativeButton={false}
              render={<Link href="/atlas/explorer" />}
            >
              <ArrowLeftIcon className="size-4" />
              Explorer
            </Button>
          </div>
        }
      />
      <PageContainer width="wide" className="flex flex-1 flex-col gap-6 pb-4">
        <section className="border-b border-border pb-4">
          <dl className="grid gap-x-8 gap-y-3 text-sm md:grid-cols-2 xl:grid-cols-3">
            <DetailRow label="Connector" value={connection?.displayName} />
            <DetailRow label="Entity" value={record.entity} mono />
            <DetailRow label="External ID" value={record.externalId} mono />
            <DetailRow
              label="External updated"
              value={
                record.externalUpdatedAt
                  ? formatDate(record.externalUpdatedAt)
                  : "Unknown"
              }
            />
            <DetailRow label="Indexed" value={formatDate(record.updatedAt)} />
            <DetailRow label="Source" value={deriveSourceMetadata(record)} />
            <DetailRow
              label="State"
              value={stringValue(raw.state) ?? stringValue(raw.default_branch)}
            />
            <DetailRow
              label="Author"
              value={
                stringValue(objectValue(raw.user)?.login) ??
                stringValue(objectValue(raw.author)?.login)
              }
            />
          </dl>
        </section>

        <section>
          <h2 className="mb-2 text-sm font-semibold">Search text</h2>
          <div className="max-h-72 overflow-auto bg-muted p-3 text-sm text-muted-foreground">
            {record.searchText}
          </div>
        </section>

        <section className="min-h-0 flex-1">
          <h2 className="mb-2 text-sm font-semibold">Raw JSON</h2>
          <pre className="max-h-[32rem] overflow-auto bg-muted p-3 text-xs text-muted-foreground">
            {formatJson(record.rawJson)}
          </pre>
        </section>
      </PageContainer>
    </>
  );
}

function DetailRow({
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
    <div className="grid gap-1">
      <dt className="text-xs font-medium text-muted-foreground">{label}</dt>
      <dd className={mono ? "font-mono text-xs" : undefined}>{value}</dd>
    </div>
  );
}

function RecordSkeleton() {
  return (
    <>
      <PageHeader group="Atlas" page="Loading..." width="wide" />
      <PageContainer width="wide" className="flex flex-1 flex-col gap-6 pb-4">
        <Skeleton className="h-20 w-full" />
        <Skeleton className="h-40 w-full" />
        <Skeleton className="h-80 w-full" />
      </PageContainer>
    </>
  );
}

function deriveSourceMetadata(record: AtlasIndexedRecord) {
  const raw = parseJsonObject(record.rawJson);
  if (record.entity === "commits") {
    return stringValue(raw.sha) ?? record.externalId;
  }
  if (record.entity === "repositories") {
    return stringValue(raw.full_name) ?? record.externalId;
  }
  const number = stringValue(raw.number);
  const state = stringValue(raw.state);
  return [number ? `#${number}` : null, state].filter(Boolean).join(" · ") ||
    record.externalId;
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

function formatDate(value: string) {
  try {
    return formatDistanceToNow(new Date(value), { addSuffix: true });
  } catch {
    return "Unknown";
  }
}
