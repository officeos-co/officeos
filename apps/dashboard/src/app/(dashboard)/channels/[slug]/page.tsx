"use client";

import { use, useState } from "react";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeftIcon } from "lucide-react";
import { PageHeader } from "@/components/page-header";
import { PageContainer } from "@/components/page-container";
import { LogDetailPanel } from "@/components/log-detail-panel";
import { LogTable } from "@/components/log-table";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useChannelConnection, useChannelLogs } from "@/features/agents";
import type { AgentLog } from "@/types/logs";

export default function ChannelDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = use(params);
  const { connection, loading } = useChannelConnection(slug);
  const { logs, loading: logsLoading } = useChannelLogs(slug);
  const [selectedLog, setSelectedLog] = useState<
    (AgentLog & { agentName?: string }) | null
  >(null);

  if (!connection) {
    if (loading) {
      return <ChannelDetailSkeleton />;
    }
    return notFound();
  }

  const selectedVisibleLog = selectedLog
    ? (logs.find((log) => log.id === selectedLog.id) ?? selectedLog)
    : null;

  return (
    <>
      <PageHeader
        group="Channels"
        page={connection.displayName}
        width="wide"
        action={
          <Button
            size="sm"
            variant="outline"
            nativeButton={false}
            render={<Link href="/channels" />}
          >
            <ArrowLeftIcon className="size-4" />
            All channels
          </Button>
        }
      />
      <PageContainer width="wide" className="flex min-h-0 flex-1 flex-col gap-4 pb-4">
        <section className="grid gap-4 border-b border-border pb-4 sm:grid-cols-[minmax(0,1fr)_auto]">
          <div className="flex min-w-0 items-start gap-3">
            {connection.logo ? (
              <span
                className="size-10 shrink-0 [&>svg]:size-10"
                dangerouslySetInnerHTML={{ __html: connection.logo }}
              />
            ) : (
              <span className="size-10 shrink-0 rounded-lg bg-muted" />
            )}
            <div className="min-w-0">
              <div className="flex flex-wrap items-center gap-2">
                <h2 className="truncate text-base font-semibold">
                  {connection.typeDisplayName}
                </h2>
                <span
                  className={
                    connection.enabled
                      ? "rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-700"
                      : "rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground"
                  }
                >
                  {connection.enabled ? "Enabled" : "Disabled"}
                </span>
              </div>
              <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
                {connection.description || "Channel resource"}
              </p>
            </div>
          </div>
          <dl className="grid grid-cols-2 gap-x-6 gap-y-1 text-sm sm:text-right">
            <dt className="text-muted-foreground">Created</dt>
            <dd>{formatDate(connection.createdAt)}</dd>
            <dt className="text-muted-foreground">Resource ID</dt>
            <dd className="font-mono text-xs">{connection.id}</dd>
          </dl>
        </section>

        <div
          className={
            selectedVisibleLog
              ? "grid min-h-0 flex-1 overflow-hidden grid-cols-[minmax(0,1fr)_clamp(360px,42vw,560px)]"
              : "flex min-h-0 flex-1 flex-col overflow-hidden"
          }
        >
          <section className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
            <div className="min-h-0 flex-1 overflow-y-auto pr-4 [scrollbar-gutter:stable]">
              <LogTable
                logs={logs}
                showAgent
                selectedLogId={selectedVisibleLog?.id ?? null}
                loading={logsLoading && logs.length === 0}
                skeletonRows={10}
                className="[&_tr]:border-0"
                onSelectLog={setSelectedLog}
              />
            </div>
          </section>

          {selectedVisibleLog && (
            <div className="h-full min-h-0 overflow-hidden border-l border-border">
              <LogDetailPanel
                log={selectedVisibleLog}
                onClose={() => setSelectedLog(null)}
                className="w-full border-l-0"
              />
            </div>
          )}
        </div>
      </PageContainer>
    </>
  );
}

function ChannelDetailSkeleton() {
  return (
    <>
      <PageHeader group="Channels" page="Loading..." width="wide" />
      <PageContainer width="wide" className="flex flex-1 flex-col gap-4 pb-4">
        <div className="flex items-start gap-3 border-b border-border pb-4">
          <Skeleton className="size-10 shrink-0 rounded-lg" />
          <div className="flex-1 space-y-2">
            <Skeleton className="h-5 w-48" />
            <Skeleton className="h-4 w-96 max-w-full" />
          </div>
        </div>
        <Skeleton className="h-96 w-full" />
      </PageContainer>
    </>
  );
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Unknown" : date.toLocaleString();
}
