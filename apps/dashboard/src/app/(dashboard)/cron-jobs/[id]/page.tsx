"use client";

import { use } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { HeartPulseIcon, Trash2Icon } from "lucide-react";
import { PageContainer } from "@/shell/page-container";
import { PageHeader } from "@/shell/page-header";
import { Button } from "@/ui/button";
import { Switch } from "@/ui/switch";
import {
  describeCronExpression,
  isHeartbeatCron,
  useCronJob,
} from "@/features/agents";

export default function CronJobDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const router = useRouter();
  const { job, loading, setCronJobEnabled, deleteCronJob } = useCronJob(id);

  async function remove() {
    await deleteCronJob();
    router.push("/cron-jobs");
  }

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page={job?.name ?? "Cron Job"}
        subtitle={
          job
            ? `Scheduled task for ${job.agentName}`
            : "Scheduled task details."
        }
        width="thin"
        action={
          <Button
            variant="outline"
            size="sm"
            nativeButton={false}
            render={<Link href="/cron-jobs" />}
          >
            All cron jobs
          </Button>
        }
      />
      <PageContainer width="thin" className="flex flex-1 flex-col pb-4">
        {!job && (
          <div className="mt-4 rounded-xl border border-border p-8 text-sm text-muted-foreground">
            {loading ? "Loading cron job..." : "Cron job not found."}
          </div>
        )}
        {job && (
          <div className="mt-4 rounded-xl border border-border">
            <div className="flex items-center gap-4 px-4 py-3">
              <Switch
                checked={job.enabled}
                onCheckedChange={(enabled) => setCronJobEnabled(enabled)}
              />
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  {isHeartbeatCron(job.expression.value) && (
                    <HeartPulseIcon className="size-3.5 text-rose-500" />
                  )}
                  <span className="text-sm font-medium">{job.name}</span>
                </div>
                <div className="mt-0.5 text-xs text-muted-foreground">
                  {describeCronExpression(job.expression.value)}
                </div>
              </div>
              <Button
                variant="ghost"
                size="icon-sm"
                className="text-muted-foreground hover:text-destructive"
                onClick={remove}
              >
                <Trash2Icon className="size-4" />
              </Button>
            </div>
            <div className="grid gap-3 border-t border-border px-4 py-3 text-sm sm:grid-cols-2">
              <div>
                <div className="text-xs text-muted-foreground">Agent</div>
                <Link
                  href={`/agents/${job.agentId}?tab=logs`}
                  className="hover:underline"
                >
                  {job.agentName}
                </Link>
              </div>
              <div>
                <div className="text-xs text-muted-foreground">Expression</div>
                <code className="font-mono text-xs">
                  {job.expression.value}
                </code>
              </div>
              <div>
                <div className="text-xs text-muted-foreground">Last run</div>
                {job.lastRunAt ? formatDate(job.lastRunAt) : "Never"}
              </div>
              <div>
                <div className="text-xs text-muted-foreground">Next run</div>
                {job.nextRunAt ? formatDate(job.nextRunAt) : "Pending"}
              </div>
            </div>
            <div className="border-t border-border px-4 py-3">
              <div className="mb-1 text-xs text-muted-foreground">Prompt</div>
              <p className="whitespace-pre-wrap text-sm">{job.prompt}</p>
            </div>
          </div>
        )}
      </PageContainer>
    </>
  );
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Unknown" : date.toLocaleString();
}
