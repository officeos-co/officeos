"use client";

import { use, useMemo } from "react";
import Link from "next/link";
import { notFound } from "next/navigation";
import dynamic from "next/dynamic";
import { formatDistanceToNow } from "date-fns";
import { ArrowLeftIcon, ExternalLinkIcon } from "lucide-react";
import type { ReactCodeMirrorProps } from "@uiw/react-codemirror";
import { json as jsonLanguage } from "@codemirror/lang-json";
import {
  defaultHighlightStyle,
  syntaxHighlighting,
} from "@codemirror/language";
import { EditorView } from "@codemirror/view";
import { PageContainer } from "@/shell/page-container";
import { PageHeader } from "@/shell/page-header";
import { Button } from "@/ui/button";
import { Skeleton } from "@/ui/skeleton";
import { cn } from "@/lib/utils";
import {
  type AtlasIndexedRecord,
  useAtlasConnection,
  useAtlasIndexedRecord,
} from "@/features/atlas";

const CodeMirror = dynamic<ReactCodeMirrorProps>(
  () => import("@uiw/react-codemirror").then((mod) => mod.default),
  { ssr: false },
);

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
          <ReadonlyCodeViewer value={record.searchText} />
        </section>

        <section className="min-h-0 flex-1">
          <h2 className="mb-2 text-sm font-semibold">Raw JSON</h2>
          <ReadonlyCodeViewer
            value={formatJson(record.rawJson)}
            language="json"
          />
        </section>
      </PageContainer>
    </>
  );
}

function ReadonlyCodeViewer({
  value,
  language = "text",
  className,
}: {
  value: string;
  language?: "json" | "text";
  className?: string;
}) {
  const extensions = useMemo(
    () => [
      ...(language === "json" ? [jsonLanguage()] : []),
      syntaxHighlighting(defaultHighlightStyle, { fallback: true }),
      EditorView.lineWrapping,
      EditorView.theme({
        "&": {
          backgroundColor: "transparent",
          color: "var(--foreground)",
          fontSize: "12px",
        },
        ".cm-scroller": {
          overflow: "visible",
          fontFamily:
            "var(--font-geist-mono), ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace",
          lineHeight: "1.5",
        },
        ".cm-content": {
          padding: "12px 0",
        },
        ".cm-line": {
          padding: "0 14px",
        },
        ".cm-gutters": {
          backgroundColor: "var(--muted)",
          color: "var(--muted-foreground)",
          borderRight: "1px solid var(--border)",
        },
        ".cm-activeLine": {
          backgroundColor: "transparent",
        },
        ".cm-activeLineGutter": {
          backgroundColor: "var(--muted)",
        },
        "&.cm-focused": {
          outline: "none",
        },
        ".cm-cursor": {
          display: "none",
        },
      }),
    ],
    [language],
  );

  return (
    <div
      className={cn(
        "overflow-hidden rounded-lg border border-input bg-background",
        className,
      )}
    >
      <CodeMirror
        value={value}
        basicSetup={{
          lineNumbers: true,
          foldGutter: language === "json",
          highlightActiveLine: false,
          highlightActiveLineGutter: false,
          bracketMatching: language === "json",
          closeBrackets: false,
          autocompletion: false,
        }}
        extensions={extensions}
        editable={false}
        theme="light"
      />
    </div>
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
