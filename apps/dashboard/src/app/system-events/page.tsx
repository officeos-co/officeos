"use client";

import { TopBar } from "@/components/shared/TopBar";
import { useSystemEvents } from "@/hooks/useSystemEvents";
import { cn } from "@/lib/utils";
import { AlertTriangle, Info, XCircle, Check } from "lucide-react";

const severityConfig = {
  error: { icon: XCircle, color: "text-red-400", bg: "bg-red-500/10", border: "border-red-500/20" },
  warning: { icon: AlertTriangle, color: "text-amber-400", bg: "bg-amber-500/10", border: "border-amber-500/20" },
  info: { icon: Info, color: "text-sky-400", bg: "bg-sky-500/10", border: "border-sky-500/20" },
} as const;

function formatTime(iso: string) {
  const d = new Date(iso);
  const now = new Date();
  const diffMs = now.getTime() - d.getTime();
  const diffMin = Math.floor(diffMs / 60000);
  if (diffMin < 1) return "just now";
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffH = Math.floor(diffMin / 60);
  if (diffH < 24) return `${diffH}h ago`;
  return d.toLocaleDateString(undefined, { month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" });
}

export default function SystemEventsPage() {
  const { events, loading, acknowledge } = useSystemEvents(100);

  return (
    <>
      <TopBar
        title="System Events"
        subtitle="Errors, warnings, and system-wide notifications"
      />

      <div className="p-8">
        {loading ? (
          <div className="flex items-center justify-center py-20">
            <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
          </div>
        ) : events.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-20 text-muted-foreground">
            <Info className="mb-3 h-8 w-8" />
            <p className="text-sm">No system events recorded yet.</p>
          </div>
        ) : (
          <div className="flex flex-col gap-2">
            {events.map((ev) => {
              const config = severityConfig[ev.severity] ?? severityConfig.info;
              const Icon = config.icon;
              return (
                <div
                  key={ev.id}
                  className={cn(
                    "flex items-start gap-3 rounded-lg border p-4 transition-colors",
                    ev.acknowledged
                      ? "border-border bg-card/50 opacity-60"
                      : cn(config.border, config.bg),
                  )}
                >
                  <Icon className={cn("mt-0.5 h-4 w-4 shrink-0", config.color)} />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 text-[13px]">
                      <span className="font-medium text-foreground">
                        {ev.message}
                      </span>
                    </div>
                    <div className="mt-1 flex flex-wrap items-center gap-3 text-[11px] text-muted-foreground">
                      <span>{formatTime(ev.createdAt)}</span>
                      <span className="rounded bg-muted px-1.5 py-0.5 font-mono">
                        {ev.category}
                      </span>
                      {ev.skillName && (
                        <span className="rounded bg-muted px-1.5 py-0.5">
                          {ev.skillName}
                        </span>
                      )}
                      {ev.correlationId && (
                        <span className="font-mono truncate max-w-[200px]">
                          {ev.correlationId}
                        </span>
                      )}
                    </div>
                  </div>
                  {!ev.acknowledged && (
                    <button
                      type="button"
                      onClick={() => acknowledge(ev.id)}
                      className="shrink-0 rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                      title="Acknowledge"
                    >
                      <Check className="h-3.5 w-3.5" />
                    </button>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>
    </>
  );
}
