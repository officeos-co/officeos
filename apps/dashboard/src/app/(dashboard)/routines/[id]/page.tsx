"use client";

import { use } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { GitBranchIcon, Trash2Icon } from "lucide-react";
import { PageContainer } from "@/shell/page-container";
import { PageHeader } from "@/shell/page-header";
import { Button } from "@/ui/button";
import { Switch } from "@/ui/switch";
import {
  latestRoutineRunAt,
  nextRoutineRunAt,
  useRoutine,
} from "@/features/agents";
import {
  describeTrigger,
  formatDateTime,
  routineTriggerSummary,
  triggerKindLabel,
} from "@/features/agents";

export default function RoutineDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const router = useRouter();
  const { routine, loading, setRoutineEnabled, deleteRoutine } = useRoutine(id);

  async function remove() {
    await deleteRoutine();
    router.push("/routines");
  }

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page={routine?.name ?? "Routine"}
        subtitle={
          routine
            ? `${routineTriggerSummary(routine)} for ${routine.agentName}`
            : "Routine details."
        }
        width="thin"
        action={
          <Button
            variant="outline"
            size="sm"
            nativeButton={false}
            render={<Link href="/routines" />}
          >
            All routines
          </Button>
        }
      />
      <PageContainer width="thin" className="flex flex-1 flex-col pb-4">
        {!routine && (
          <div className="mt-4 rounded-lg border border-border p-8 text-sm text-muted-foreground">
            {loading ? "Loading routine..." : "Routine not found."}
          </div>
        )}
        {routine && (
          <div className="mt-4 rounded-lg border border-border">
            <div className="flex items-center gap-4 px-4 py-3">
              <Switch
                checked={routine.enabled}
                onCheckedChange={(enabled) => setRoutineEnabled(enabled)}
              />
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <GitBranchIcon className="size-3.5 text-muted-foreground" />
                  <span className="text-sm font-medium">{routine.name}</span>
                </div>
                <div className="mt-0.5 text-xs text-muted-foreground">
                  {routineTriggerSummary(routine)}
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
                  href={`/agents/${routine.agentId}?tab=logs`}
                  className="hover:underline"
                >
                  {routine.agentName}
                </Link>
              </div>
              <div>
                <div className="text-xs text-muted-foreground">Created</div>
                {formatDateTime(routine.createdAt)}
              </div>
              <div>
                <div className="text-xs text-muted-foreground">Last run</div>
                {formatDateTime(latestRoutineRunAt(routine))}
              </div>
              <div>
                <div className="text-xs text-muted-foreground">Next run</div>
                {nextRoutineRunAt(routine)
                  ? formatDateTime(nextRoutineRunAt(routine))
                  : "Pending"}
              </div>
            </div>
            <div className="border-t border-border px-4 py-3">
              <div className="mb-2 text-xs text-muted-foreground">Triggers</div>
              <div className="space-y-3">
                {routine.triggers.map((trigger) => (
                  <div key={trigger.id} className="rounded-md bg-muted/40 p-3">
                    <div className="text-sm font-medium">
                      {triggerKindLabel(trigger.kind)}: {trigger.name}
                    </div>
                    <div className="mt-1 text-sm text-muted-foreground">
                      {describeTrigger(trigger)}
                    </div>
                  </div>
                ))}
              </div>
            </div>
            <div className="border-t border-border px-4 py-3">
              <div className="mb-1 text-xs text-muted-foreground">Prompt</div>
              <p className="whitespace-pre-wrap text-sm">{routine.prompt}</p>
            </div>
          </div>
        )}
      </PageContainer>
    </>
  );
}
